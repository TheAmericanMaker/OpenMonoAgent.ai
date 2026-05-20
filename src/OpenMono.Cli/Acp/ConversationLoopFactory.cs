using OpenMono.Config;
using OpenMono.Llm;
using OpenMono.Permissions;
using OpenMono.Rendering;
using OpenMono.Session;
using OpenMono.Tools;

namespace OpenMono.Acp;

/// <summary>
/// Builds <see cref="ConversationLoop"/> instances configured for ACP turns. Holds the
/// constructor-bound dependencies (LLM client, tool registry, app config, etc.) so
/// <see cref="AcpTurnRunner"/> only has to supply the per-turn arguments
/// (session state, event sink, user-interaction forwarder).
/// </summary>
public sealed class ConversationLoopFactory
{
    private readonly ILlmClient _llm;
    private readonly ToolRegistry _tools;
    private readonly AppConfig _config;
    private readonly IOutputSink _output;
    private readonly IInputReader _input;
    private readonly ILiveFeedback? _liveFeedback;

    public ConversationLoopFactory(
        ILlmClient llm,
        ToolRegistry tools,
        AppConfig config,
        IOutputSink output,
        IInputReader input,
        ILiveFeedback? liveFeedback = null)
    {
        _llm = llm;
        _tools = tools;
        _config = config;
        _output = output;
        _input = input;
        _liveFeedback = liveFeedback;
    }

    /// <summary>
    /// Construct a loop wired for an ACP turn. The supplied <paramref name="interaction"/>
    /// causes ConversationLoop to swap in <see cref="AcpInputReaderAdapter"/> and rebuild
    /// PermissionEngine over it (see <c>ConversationLoop</c>'s ACP-mode branch). The
    /// <paramref name="placeholderPermissions"/> argument is unused in that branch but
    /// must be non-null to satisfy the constructor signature.
    /// </summary>
    public ConversationLoop Create(SessionState session, IAcpEventSink sink, IAcpUserInteraction interaction)
    {
        var placeholderPermissions = new PermissionEngine(_config, _output, _input);
        return new ConversationLoop(
            _llm,
            _tools,
            placeholderPermissions,
            _output,
            _input,
            _liveFeedback,
            _config,
            session,
            sink: sink,
            interaction: interaction);
    }
}
