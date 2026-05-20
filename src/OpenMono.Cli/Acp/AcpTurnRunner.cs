using System.Text.Json;
using OpenMono.Session;

namespace OpenMono.Acp;

/// <summary>
/// Owns one HTTP /turn request lifetime. Builds the per-request
/// <see cref="IAcpEventSink"/> (this class) and <see cref="IAcpUserInteraction"/>
/// (an <see cref="AcpUserInteractionForwarder"/>) pointed at the request's
/// <see cref="SseWriter"/>, then runs a ConversationLoop.
///
/// Pause-resume: a paused permission / user_input throws
/// <see cref="PendingUserResponseException"/> out of ConversationLoop; the runner
/// catches it, syncs state back to AcpSession, and closes the SSE stream. The next
/// /turn POST resolves the pause (recording the answer in the session-scoped cache),
/// appends a synthetic Tool message so the LLM API accepts the resumed history, and
/// re-enters via ContinueTurnAsync.
///
/// v1 simplification: when an assistant message has multiple unresolved tool_use
/// blocks, only the first one is paired with the resolved decision; the rest receive
/// a generic "deferred — retry to execute" Tool message. Multi-tool turns are rare
/// in practice (writes tend to be solitary) and the LLM's next response can re-issue
/// the deferred ones, which the remembered-decision cache then resolves without a
/// second pause.
/// </summary>
public sealed class AcpTurnRunner : IAcpEventSink
{
    private readonly AcpSession _acpSession;
    private readonly SseWriter _writer;
    private readonly ConversationLoopFactory _loopFactory;
    private readonly AcpServerSettings _settings;
    private readonly IAcpUserInteraction _interaction;

    public AcpTurnRunner(
        AcpSession session,
        SseWriter writer,
        ConversationLoopFactory loopFactory,
        AcpServerSettings settings)
    {
        _acpSession = session;
        _writer = writer;
        _loopFactory = loopFactory;
        _settings = settings;
        _interaction = new AcpUserInteractionForwarder(session, writer, settings.PendingUserResponseTimeout);
    }

    // ── Entry points ───────────────────────────────────────────────────────────

    public async Task RunUserMessageAsync(string userText, CancellationToken ct)
    {
        _acpSession.Messages.Add(new Message { Role = MessageRole.User, Content = userText });
        _acpSession.TurnCount++;
        await DriveLoopAsync(ct);
    }

    public async Task ResumeWithPermissionAsync(JsonElement payload, CancellationToken ct)
    {
        var id = payload.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("permission_response missing `id`");
        var decision = payload.TryGetProperty("decision", out var dEl) ? dEl.GetString() : null;
        var allow = string.Equals(decision, "allow", StringComparison.Ordinal);

        var ctx = _acpSession.LookupPauseContext(id)
            ?? throw new InvalidOperationException($"permission_response for unknown or already-resolved pause id: {id}");
        if (ctx.Kind != PendingResponseKind.Permission)
            throw new InvalidOperationException($"pause {id} is not a Permission pause (was {ctx.Kind})");

        if (!_acpSession.TryResolvePause(id, new AcpPermissionResponse(allow)))
            throw new InvalidOperationException($"failed to resolve pause id: {id}");

        // Remember the decision so the LLM's re-issued tool call on resume doesn't
        // re-pause: AcpUserInteractionForwarder.RequestPermissionAsync checks this
        // cache first and returns the value without emitting another event.
        _acpSession.RememberPermission(ctx.ContextKey, allow);

        AppendSyntheticToolMessages(allow
            ? "Permission granted by user. Re-issue the tool call to execute."
            : "Permission denied by user.");

        await DriveLoopAsync(ct);
    }

    public async Task ResumeWithUserInputAsync(JsonElement payload, CancellationToken ct)
    {
        var id = payload.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("user_input_response missing `id`");
        var value = payload.TryGetProperty("value", out var vEl) ? vEl.GetString() ?? "" : "";

        var ctx = _acpSession.LookupPauseContext(id)
            ?? throw new InvalidOperationException($"user_input_response for unknown or already-resolved pause id: {id}");
        if (ctx.Kind != PendingResponseKind.UserInput)
            throw new InvalidOperationException($"pause {id} is not a UserInput pause (was {ctx.Kind})");

        if (!_acpSession.TryResolvePause(id, new AcpUserInputResponse(value)))
            throw new InvalidOperationException($"failed to resolve pause id: {id}");

        _acpSession.RememberUserInput(ctx.ContextKey, value);

        // For AskUser, the answer IS the tool's result — no "re-issue" needed.
        AppendSyntheticToolMessages(value);

        await DriveLoopAsync(ct);
    }

    public void AbortPendingPauses()
    {
        _acpSession.CancelAllPending();
    }

    // ── Loop driver ────────────────────────────────────────────────────────────

    private async Task DriveLoopAsync(CancellationToken ct)
    {
        var sessionState = BuildSessionState();
        using var loop = _loopFactory.Create(sessionState, sink: this, interaction: _interaction);

        try
        {
            // ContinueTurnAsync (rather than RunTurnAsync) — the User message and TurnCount
            // bump already happened in RunUserMessageAsync, and resumed turns inherit the
            // session as-is.
            await loop.ContinueTurnAsync(ct);
            SyncBackToAcpSession(sessionState);
            await _writer.WriteEventAsync("done", new { });
        }
        catch (PendingUserResponseException)
        {
            // Forwarder already wrote permission_request / user_input_request before throwing.
            // Sync state but skip the `done` event; the stream closes here and the next /turn
            // POST resolves the pause and re-enters DriveLoopAsync.
            SyncBackToAcpSession(sessionState);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnect or explicit abort. Session stays valid for resume.
            SyncBackToAcpSession(sessionState);
        }
        catch (Exception e)
        {
            SyncBackToAcpSession(sessionState);
            await _writer.WriteEventAsync("error", new { message = e.Message });
        }
    }

    // ── History plumbing ───────────────────────────────────────────────────────

    /// <summary>
    /// Append synthetic Tool messages for every unresolved tool_use in the last
    /// assistant message. The first unresolved tool_use carries the actual
    /// <paramref name="resolutionContent"/>; the rest receive a generic
    /// "Execution deferred" so the LLM API accepts the history shape.
    /// </summary>
    private void AppendSyntheticToolMessages(string resolutionContent)
    {
        var lastAssistant = _acpSession.Messages
            .LastOrDefault(m => m.Role == MessageRole.Assistant && m.ToolCalls is not null);
        if (lastAssistant?.ToolCalls is null || lastAssistant.ToolCalls.Count == 0) return;

        var alreadyAnswered = _acpSession.Messages
            .Where(m => m.Role == MessageRole.Tool && m.ToolCallId is not null)
            .Select(m => m.ToolCallId!)
            .ToHashSet();

        var first = true;
        foreach (var call in lastAssistant.ToolCalls)
        {
            if (alreadyAnswered.Contains(call.Id)) continue;
            _acpSession.Messages.Add(new Message
            {
                Role = MessageRole.Tool,
                ToolCallId = call.Id,
                ToolName = call.Name,
                Content = first ? resolutionContent : "Execution deferred. Retry to run.",
            });
            first = false;
        }
    }

    private SessionState BuildSessionState()
    {
        var ss = new SessionState();
        foreach (var m in _acpSession.Messages) ss.AddMessage(m);
        ss.TurnCount = _acpSession.TurnCount;
        ss.Meta.PlanMode = _acpSession.PlanMode;
        ss.Todos.Clear();
        foreach (var t in _acpSession.Todos) ss.Todos.Add(t);
        ss.Meta.TokenTracker ??= new TokenTracker();
        return ss;
    }

    private void SyncBackToAcpSession(SessionState ss)
    {
        _acpSession.Messages.Clear();
        _acpSession.Messages.AddRange(ss.Messages);
        _acpSession.PlanMode = ss.Meta.PlanMode;
        _acpSession.Todos.Clear();
        foreach (var t in ss.Todos) _acpSession.Todos.Add(t);
    }

    // ── IAcpEventSink: every method forwards to a single SSE event ─────────────

    public Task OnTextDeltaAsync(string content)
        => _writer.WriteEventAsync("text_delta", new { content });

    public Task OnThinkingDeltaAsync(string content)
        => _writer.WriteEventAsync("thinking_delta", new { content });

    public Task OnToolStartAsync(string callId, string name, string summary)
        => _writer.WriteEventAsync("tool_start", new { id = callId, name, summary });

    public Task OnToolEndAsync(string callId, string name, bool ok, double durationMs)
        => _writer.WriteEventAsync("tool_end", new { id = callId, name, ok, duration_ms = durationMs });

    public Task OnToolResultPreviewAsync(string callId, string preview, string? artifactId)
        => _writer.WriteEventAsync("tool_result_preview", new
        {
            id = callId,
            preview,
            artifact_id = artifactId,
        });

    public Task OnCompactionAsync(int messagesCompressed, double durationSeconds, int checkpointIndex)
        => _writer.WriteEventAsync("compaction", new
        {
            messages_compressed = messagesCompressed,
            duration_seconds = durationSeconds,
            checkpoint_index = checkpointIndex,
        });

    public Task OnUsageAsync(int inputTokens, int outputTokens, int totalTokens)
        => _writer.WriteEventAsync("usage", new
        {
            input_tokens = inputTokens,
            output_tokens = outputTokens,
            total_tokens = totalTokens,
        });
}
