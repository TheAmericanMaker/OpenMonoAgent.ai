namespace OpenMono.Session;

/// <summary>
/// Reattaches a live <see cref="SessionState"/> to a previously-saved session,
/// in place. The live object is mutated (not replaced) so that components holding it
/// by reference — the TUI renderer and the conversation loop — stay consistent, and
/// subsequent saves continue writing to the resumed session's file (stable identity).
/// The current system prompt is preserved; the loaded conversation is restored and
/// repaired for any tool call interrupted mid-turn.
/// </summary>
public static class SessionReattach
{
    public static void Apply(SessionState live, SessionState loaded)
    {
        var currentSystem = live.Messages.FirstOrDefault(m => m.Role == MessageRole.System);

        live.Id = loaded.Id;
        live.StartedAt = loaded.StartedAt;
        live.Model = loaded.Model;

        live.Messages.Clear();
        if (currentSystem is not null)
            live.Messages.Add(currentSystem);
        foreach (var msg in loaded.Messages.Where(m => m.Role != MessageRole.System))
            live.Messages.Add(msg);

        live.TurnCount = loaded.TurnCount;
        live.TotalTokensUsed = loaded.TotalTokensUsed;

        live.Checkpoints.Clear();
        foreach (var cp in loaded.Checkpoints)
            live.Checkpoints.Add(cp);
        live.CheckpointCutoffIndex = loaded.CheckpointCutoffIndex;

        live.Todos.Clear();
        foreach (var todo in loaded.Todos)
            live.Todos.Add(todo);

        live.Meta.PlanMode = loaded.Meta.PlanMode;

        SessionConsistency.Repair(live);
    }
}
