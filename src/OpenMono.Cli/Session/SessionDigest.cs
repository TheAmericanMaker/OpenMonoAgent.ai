namespace OpenMono.Session;

/// <summary>
/// Cheap, LLM-free derivation of human-readable session digest fields used by the
/// resume picker (TUI and ACP). No model calls — title comes from the first user
/// message, the latest summary reuses the most recent checkpoint.
/// </summary>
public static class SessionDigest
{
    public static string DeriveTitle(IReadOnlyList<Message> messages, int maxLength = 80)
    {
        var first = messages.FirstOrDefault(m => m.Role == MessageRole.User)?.Content;
        if (string.IsNullOrWhiteSpace(first)) return "";

        var collapsed = string.Join(' ', first.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length <= maxLength) return collapsed;
        return collapsed[..maxLength] + "…";
    }

    public static string? DeriveLatestSummary(IReadOnlyList<CheckpointEntry> checkpoints)
        => checkpoints.Count > 0 ? checkpoints[^1].Summary : null;
}
