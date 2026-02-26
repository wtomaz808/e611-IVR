using PstnSimulator.Models;

namespace PstnSimulator.Services;

/// <summary>
/// Simulates the PSTN layer — trunk management, channel allocation, call origination.
/// </summary>
public class PstnService
{
    private readonly List<GatewayConfig> _gateways;
    private readonly List<TrunkConfig> _trunks;
    private readonly List<SimulatedDid> _dids;
    private readonly List<SimulatedCaller> _callers;
    private readonly ILogger<PstnService> _logger;
    private readonly object _lock = new();

    public PstnService(IConfiguration config, ILogger<PstnService> logger)
    {
        _logger = logger;

        var pstnConfig = new PstnConfig();
        config.GetSection("Pstn").Bind(pstnConfig);

        _gateways = pstnConfig.Gateways;
        _trunks = pstnConfig.Trunks;
        _dids = pstnConfig.Dids;
        _callers = pstnConfig.Callers;
    }

    public IReadOnlyList<GatewayConfig> Gateways => _gateways;
    public IReadOnlyList<TrunkConfig> Trunks => _trunks;
    public IReadOnlyList<SimulatedDid> Dids => _dids;
    public IReadOnlyList<SimulatedCaller> Callers => _callers;

    public IReadOnlyList<GatewayConfig> GetGateways() => _gateways;
    public IReadOnlyList<TrunkConfig> GetTrunks() => _trunks;
    public IReadOnlyList<SimulatedDid> GetDids() => _dids;
    public IReadOnlyList<SimulatedCaller> GetCallers() => _callers;

    /// <summary>
    /// Attempt to seize a channel on the carrier trunk for an inbound PSTN call.
    /// Returns trunk name and channel — or null if unavailable.
    /// </summary>
    public (string TrunkName, int Channel)? SeizeInboundChannel(string didNumber)
    {
        lock (_lock)
        {
            var did = _dids.FirstOrDefault(d => d.Number == didNumber);
            if (did == null) return null;

            var trunk = _trunks.FirstOrDefault(t => t.Name == did.TrunkName);
            if (trunk == null) return null;

            if (!trunk.HasCapacity)
            {
                _logger.LogWarning("Inbound trunk {Trunk} is full ({Active}/{Capacity})",
                    trunk.Name, trunk.ActiveChannels, trunk.Capacity);
                return null;
            }

            trunk.ActiveChannels++;
            var channel = trunk.ActiveChannels;

            _logger.LogInformation("Seized inbound channel {Channel} on carrier trunk {Trunk} for DID {Did}",
                channel, trunk.Name, didNumber);

            return (trunk.Name, channel);
        }
    }

    /// <summary>
    /// After CM10 processes the call (VDN/vector), seize the outbound trunk through the gateway/SBC to ACS.
    /// Checks gateway registration status before allowing the call to proceed.
    /// Returns trunk name, channel, and gateway name — or null if unavailable.
    /// </summary>
    public (string TrunkName, int Channel, string GatewayName)? SeizeOutboundChannel()
    {
        lock (_lock)
        {
            // Find a trunk that routes through a registered gateway
            foreach (var gw in _gateways.Where(g => g.Registered))
            {
                foreach (var trunkName in gw.TrunkNames)
                {
                    var trunk = _trunks.FirstOrDefault(t => t.Name == trunkName);
                    if (trunk != null && trunk.HasCapacity)
                    {
                        trunk.ActiveChannels++;
                        var channel = trunk.ActiveChannels;

                        _logger.LogInformation(
                            "Seized outbound channel {Channel} on trunk {Trunk} via gateway {Gw}",
                            channel, trunk.Name, gw.Name);

                        return (trunk.Name, channel, gw.Name);
                    }
                }
            }

            _logger.LogWarning("No outbound gateway/trunk available — all gateways unregistered or trunks full");
            return null;
        }
    }

    /// <summary>
    /// Attempt to seize a channel on the trunk assigned to the target DID (legacy compatibility).
    /// Returns trunk name, channel, and gateway name — or null if unavailable.
    /// </summary>
    public (string TrunkName, int Channel, string? GatewayName)? SeizeChannel(string didNumber)
    {
        lock (_lock)
        {
            var did = _dids.FirstOrDefault(d => d.Number == didNumber);
            if (did == null) return null;

            var trunk = _trunks.FirstOrDefault(t => t.Name == did.TrunkName);
            if (trunk == null) return null;

            // Check gateway registration if the trunk has one
            if (!string.IsNullOrEmpty(trunk.GatewayName))
            {
                var gateway = _gateways.FirstOrDefault(g => g.Name == trunk.GatewayName);
                if (gateway != null && !gateway.Registered)
                {
                    _logger.LogWarning("Gateway {Gateway} is not registered — cannot seize trunk {Trunk}",
                        gateway.Name, trunk.Name);
                    return null;
                }
            }

            if (!trunk.HasCapacity)
            {
                _logger.LogWarning("Trunk {Trunk} is full ({Active}/{Capacity})",
                    trunk.Name, trunk.ActiveChannels, trunk.Capacity);
                return null;
            }

            trunk.ActiveChannels++;
            var channel = trunk.ActiveChannels;

            _logger.LogInformation("Seized channel {Channel} on trunk {Trunk} (gateway: {Gw}) for DID {Did}",
                channel, trunk.Name, trunk.GatewayName ?? "none", didNumber);

            return (trunk.Name, channel, trunk.GatewayName);
        }
    }

    /// <summary>
    /// Release a channel on a trunk (call ended).
    /// </summary>
    public void ReleaseChannel(string trunkName)
    {
        lock (_lock)
        {
            var trunk = _trunks.FirstOrDefault(t => t.Name == trunkName);
            if (trunk != null && trunk.ActiveChannels > 0)
            {
                trunk.ActiveChannels--;
                _logger.LogInformation("Released channel on trunk {Trunk} ({Active}/{Capacity})",
                    trunk.Name, trunk.ActiveChannels, trunk.Capacity);
            }
        }
    }

    /// <summary>
    /// Look up the VDN number mapped to a DID.
    /// </summary>
    public string? GetVdnForDid(string didNumber)
    {
        return _dids.FirstOrDefault(d => d.Number == didNumber)?.VdnNumber;
    }

    /// <summary>
    /// Check if a caller is blocked.
    /// </summary>
    public bool IsCallerBlocked(string callerNumber)
    {
        var caller = _callers.FirstOrDefault(c => c.Number == callerNumber);
        return caller?.Type == CallerType.Blocked;
    }
}
