using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using OpenMono.Session;

namespace OpenMono.Acp;

/// <summary>
/// Per-client ACP session. Persisted fields capture only the state the agent needs to
/// resume a conversation after a container restart; the pause-resume primitives
/// (TurnLock, _pending) are runtime-only and rebuilt on every load.
/// </summary>
public sealed class AcpSession
{
    public required string Id { get; init; }
    public required DateTime StartedAt { get; init; }
    public DateTime LastActivityAt { get; set; }
    public required string Model { get; init; }
    public int TurnCount { get; set; }
    public bool PlanMode { get; set; }
    public List<TodoItem> Todos { get; init; } = new();
    public List<Message> Messages { get; init; } = new();

    /// <summary>Serializes the single turn-in-flight invariant. One /turn per session at a time.</summary>
    [JsonIgnore]
    public SemaphoreSlim TurnLock { get; } = new(1, 1);

    /// <summary>
    /// Pause-resume registry. Keyed by pause id (e.g. <c>perm_abc</c> / <c>ask_xyz</c>).
    /// Runtime-only: a paused conversation cannot survive a container restart because the
    /// awaiting TaskCompletionSource lives only in this process. <c>ContextKey</c> stashes
    /// what was asked (e.g. <c>"Bash|rm /tmp/file"</c> or the AskUser question text) so
    /// AcpTurnRunner can remember the decision under that key for the resumed loop's
    /// duplicate prompt.
    /// </summary>
    [JsonIgnore]
    private readonly ConcurrentDictionary<string, PendingPause> _pending = new();

    [JsonIgnore]
    private readonly ConcurrentDictionary<string, bool> _rememberedPermissions = new();

    [JsonIgnore]
    private readonly ConcurrentDictionary<string, string> _rememberedUserInputs = new();

    public TaskCompletionSource<AcpPauseResponse> RegisterPause(
        string id, PendingResponseKind kind, string contextKey)
    {
        var tcs = new TaskCompletionSource<AcpPauseResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(id, new PendingPause(kind, contextKey, tcs)))
            throw new InvalidOperationException($"Duplicate pause id: {id}");
        return tcs;
    }

    public bool TryResolvePause(string id, AcpPauseResponse response)
        => _pending.TryRemove(id, out var pp) && pp.Tcs.TrySetResult(response);

    public (PendingResponseKind Kind, string ContextKey)? LookupPauseContext(string id)
        => _pending.TryGetValue(id, out var pp) ? (pp.Kind, pp.ContextKey) : null;

    [JsonIgnore]
    public IReadOnlyCollection<string> PendingIds => _pending.Keys.ToArray();

    public void CancelAllPending()
    {
        foreach (var kv in _pending) kv.Value.Tcs.TrySetCanceled();
        _pending.Clear();
    }

    /// <summary>
    /// Stash a permission decision (<c>true</c> = Allow) under the context key the original
    /// pause was registered with. The <see cref="AcpUserInteractionForwarder"/> consults this
    /// cache before pausing again, so the LLM's re-issued tool call after a resume picks up
    /// the same decision without another round-trip to the client.
    /// </summary>
    public void RememberPermission(string contextKey, bool allow)
        => _rememberedPermissions[contextKey] = allow;

    public bool? TryGetRememberedPermission(string contextKey)
        => _rememberedPermissions.TryGetValue(contextKey, out var v) ? v : null;

    public void RememberUserInput(string contextKey, string value)
        => _rememberedUserInputs[contextKey] = value;

    public string? TryGetRememberedUserInput(string contextKey)
        => _rememberedUserInputs.TryGetValue(contextKey, out var v) ? v : null;

    private sealed record PendingPause(
        PendingResponseKind Kind,
        string ContextKey,
        TaskCompletionSource<AcpPauseResponse> Tcs);
}

/// <summary>
/// Client response to a paused turn. The base type lives in OpenMono.Acp to avoid colliding
/// with <see cref="OpenMono.Permissions.PermissionResponse"/> (an enum) — every concrete
/// subtype here is prefixed with <c>Acp</c> for the same reason.
/// </summary>
public abstract record AcpPauseResponse;

public sealed record AcpPermissionResponse(bool Allow) : AcpPauseResponse;

public sealed record AcpUserInputResponse(string Value) : AcpPauseResponse;

public sealed record AcpCancelledResponse() : AcpPauseResponse;
