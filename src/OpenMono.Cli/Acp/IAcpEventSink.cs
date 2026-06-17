namespace OpenMono.Acp;






public interface IAcpEventSink
{
    Task OnTextDeltaAsync(string content);
    Task OnThinkingDeltaAsync(string content);


    Task OnToolStartAsync(string callId, string name, string summary);


    Task OnToolEndAsync(string callId, string name, bool ok, double durationMs);

    Task OnToolResultPreviewAsync(string callId, string preview, string? artifactId);
    Task OnCompactionAsync(int messagesCompressed, double durationSeconds, int checkpointIndex);
    Task OnUsageAsync(int inputTokens, int outputTokens, int totalTokens);

    // Progress lines forwarded from sub-agents via WriteOutput/StreamText.
    // These are distinct from the main assistant text stream.
    // Fire-and-forget from synchronous ToolContext delegates; order is best-effort.
    Task OnSubAgentLogAsync(string line);
}
