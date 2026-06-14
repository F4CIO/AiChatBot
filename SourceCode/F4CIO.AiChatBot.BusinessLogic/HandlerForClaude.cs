using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Core;
using F4CIO.AiChatBot.Common;
using F4CIO.AiChatBot.Common.Entities;
using AAgents = Anthropic.Models.Beta.Agents;
using AEnv = Anthropic.Models.Beta.Environments;
using AMem = Anthropic.Models.Beta.MemoryStores;
using ASess = Anthropic.Models.Beta.Sessions;
using AEvents = Anthropic.Models.Beta.Sessions.Events;

namespace F4CIO.AiChatBot.BusinessLogic;

/// <summary>
/// All Claude Managed Agents integration lives here (the official `Anthropic` C# SDK, beta surface).
/// At startup it finds-or-creates the configured Environment, Memory store and Agent; per
/// conversation it opens a Session with the Memory store attached; per message it sends a user
/// event and streams the agent's reply to completion (synchronous request/reply).
/// </summary>
internal sealed partial class HandlerForClaude
{
    private readonly Configuration _cfg;
    private readonly AnthropicClient _client;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private bool _ready;
    private string _environmentId = "";
    private string _memoryStoreId = "";
    private string _agentId = "";

    public HandlerForClaude(Configuration cfg)
    {
        _cfg = cfg;
        _client = new AnthropicClient
        {
            ApiKey = cfg.ClaudeApiKey,
            BaseUrl = string.IsNullOrWhiteSpace(cfg.ClaudeBaseUrl) ? "https://api.anthropic.com" : cfg.ClaudeBaseUrl,
        };
    }

    /// <summary>Find-or-create the Environment, Memory store and Agent. Idempotent; runs once.</summary>
    public async Task EnsureReadyAsync(CancellationToken ct = default)
    {
        if (_ready) return;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_ready) return;
            _environmentId = await FindOrCreateEnvironmentAsync(ct);
            _memoryStoreId = await FindOrCreateMemoryStoreAsync(ct);
            _agentId = await FindOrCreateAgentAsync(ct);
            _ready = true;
            HandlerForLogging.AddLine(
                $"Claude ready: env={_environmentId}, agent={_agentId}, memoryStore={_memoryStoreId}");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<string> FindOrCreateEnvironmentAsync(CancellationToken ct)
    {
        var list = await _client.Beta.Environments.List(new AEnv.EnvironmentListParams(), ct);
        var found = list.Items.FirstOrDefault(e => e.Name == _cfg.ClaudeEnvironmentName);
        if (found is not null) return found.ID;

        var created = await _client.Beta.Environments.Create(new AEnv.EnvironmentCreateParams
        {
            Name = _cfg.ClaudeEnvironmentName,
            Config = new AEnv.Config(
                new AEnv.BetaCloudConfigParams
                {
                    Networking = new AEnv.BetaCloudConfigParamsNetworking(new AEnv.BetaUnrestrictedNetwork(), null),
                },
                null),
        }, ct);
        HandlerForLogging.AddLine($"Created Claude environment '{_cfg.ClaudeEnvironmentName}' ({created.ID})");
        return created.ID;
    }

    private async Task<string> FindOrCreateMemoryStoreAsync(CancellationToken ct)
    {
        var list = await _client.Beta.MemoryStores.List(new AMem.MemoryStoreListParams(), ct);
        var found = list.Items.FirstOrDefault(m => m.Name == _cfg.ClaudeMemoryStoreName);
        if (found is not null) return found.ID;

        var created = await _client.Beta.MemoryStores.Create(new AMem.MemoryStoreCreateParams
        {
            Name = _cfg.ClaudeMemoryStoreName,
            Description = "Long-term memory for the F4CIO AI chatbot: user context and preferences.",
        }, ct);
        HandlerForLogging.AddLine($"Created Claude memory store '{_cfg.ClaudeMemoryStoreName}' ({created.ID})");
        return created.ID;
    }

    private async Task<string> FindOrCreateAgentAsync(CancellationToken ct)
    {
        var list = await _client.Beta.Agents.List(new AAgents.AgentListParams(), ct);
        var found = list.Items.FirstOrDefault(a => a.Name == _cfg.ClaudeAgentName);
        if (found is not null) return found.ID;

        ApiEnum<string, AAgents.BetaManagedAgentsModel> model = _cfg.ClaudeModel;
        var agentParams = new AAgents.AgentCreateParams
        {
            Name = _cfg.ClaudeAgentName,
            Model = new AAgents.Model(model, null),
            System = "You are a helpful, concise chatbot assistant. Use your memory to remember "
                   + "useful context about the user across the conversation, and recall it when relevant.",
            Tools = new List<AAgents.Tool>
            {
                new AAgents.Tool(new AAgents.BetaManagedAgentsAgentToolset20260401Params
                {
                    Type = AAgents.BetaManagedAgentsAgentToolset20260401ParamsType.AgentToolset20260401,
                }, null),
            },
        };

        var created = await _client.Beta.Agents.Create(agentParams, ct);
        HandlerForLogging.AddLine($"Created Claude agent '{_cfg.ClaudeAgentName}' ({created.ID})");
        return created.ID;
    }

    /// <summary>Open a new session for a conversation, attaching the memory store at session start.</summary>
    public async Task<string> StartSessionAsync(CancellationToken ct = default)
    {
        await EnsureReadyAsync(ct);

        var session = await _client.Beta.Sessions.Create(new ASess.SessionCreateParams
        {
            Agent = new ASess.Agent(_agentId, null),
            EnvironmentID = _environmentId,
            Title = "F4CIO chatbot session",
            Resources = new List<ASess.Resource>
            {
                new ASess.Resource(
                    new ASess.BetaManagedAgentsMemoryStoreResourceParam
                    {
                        MemoryStoreID = _memoryStoreId,
                        Instructions = "User context and preferences. Check before answering and update as you learn.",
                        Type = ASess.BetaManagedAgentsMemoryStoreResourceParamType.MemoryStore,
                    },
                    null),
            },
        }, ct);
        return session.ID;
    }

    /// <summary>
    /// Send a user message and stream the session events until the turn completes, returning the
    /// agent's concatenated reply. Our agent uses the default (auto-allowed) built-in toolset and no
    /// custom tools, so an idle event always means the turn is finished.
    /// </summary>
    public async Task<string> SendAndGetReplyAsync(string sessionId, string userText, CancellationToken ct = default)
    {
        var sendParams = new AEvents.EventSendParams
        {
            Events = new List<AEvents.BetaManagedAgentsEventParams>
            {
                new AEvents.BetaManagedAgentsEventParams(
                    new AEvents.BetaManagedAgentsUserMessageEventParams
                    {
                        Type = AEvents.BetaManagedAgentsUserMessageEventParamsType.UserMessage,
                        Content = new List<AEvents.BetaManagedAgentsUserMessageEventParamsContent>
                        {
                            new AEvents.BetaManagedAgentsUserMessageEventParamsContent(
                                new AEvents.BetaManagedAgentsTextBlock
                                {
                                    Text = userText,
                                    Type = AEvents.BetaManagedAgentsTextBlockType.Text,
                                }, null),
                        },
                    },
                    null),
            },
        };
        await _client.Beta.Sessions.Events.Send(sessionId, sendParams, ct);

        var reply = new StringBuilder();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(3));

        await foreach (var ev in _client.Beta.Sessions.Events.StreamStreaming(
                           sessionId, new AEvents.EventStreamParams(), timeoutCts.Token))
        {
            if (ev.TryPickAgentMessageEvent(out var msg) && msg is not null)
            {
                foreach (var block in msg.Content) reply.Append(block.Text);
            }
            else if (ev.TryPickSessionErrorEvent(out _))
            {
                throw new InvalidOperationException("Claude session error: " + ev.Json.GetRawText());
            }
            else if (ev.TryPickSessionStatusIdleEvent(out _) || ev.TryPickSessionStatusTerminatedEvent(out _))
            {
                break;
            }
        }

        return reply.ToString().Trim();
    }
}
