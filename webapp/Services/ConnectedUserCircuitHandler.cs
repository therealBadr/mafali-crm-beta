using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace MafaliCrm.Web.Services;

public sealed class ConnectedUserCircuitHandler : CircuitHandler, IDisposable
{
    private readonly ConnectedUserRegistry _registry;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly CircuitContext _circuitContext;
    private readonly ILogger<ConnectedUserCircuitHandler> _logger;

    private string? _circuitId;
    private bool _subscribed;

    private string? _lastLogin;
    private bool _everApplied;
    private readonly object _applyGate = new();

    public ConnectedUserCircuitHandler(
        ConnectedUserRegistry registry,
        AuthenticationStateProvider authStateProvider,
        CircuitContext circuitContext,
        ILogger<ConnectedUserCircuitHandler> logger)
    {
        _registry = registry;
        _authStateProvider = authStateProvider;
        _circuitContext = circuitContext;
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _circuitId = circuit.Id;
        _registry.CircuitOpened(circuit.Id);

        _circuitContext.CircuitId = circuit.Id;

        _authStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        _subscribed = true;

        _logger.LogDebug("Circuit {CircuitId} opened (provider {Provider}); {Count} circuit(s) tracked",
            circuit.Id, _authStateProvider.GetType().Name, _registry.CircuitCount);

        TryReadLoginNow();

        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _registry.ConnectionUp(circuit.Id);
        TryReadLoginNow();
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _registry.ConnectionDown(circuit.Id);
        _logger.LogDebug("Circuit {CircuitId} disconnected (kept alive pending reconnect)", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Unsubscribe();
        _registry.CircuitClosed(circuit.Id);
        _logger.LogInformation("Circuit {CircuitId} closed; {Count} circuit(s) still tracked",
            circuit.Id, _registry.CircuitCount);
        return Task.CompletedTask;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) => ApplyWhenReady(task);

    private void TryReadLoginNow()
    {
        try
        {
            ApplyWhenReady(_authStateProvider.GetAuthenticationStateAsync());
        }
        catch (InvalidOperationException)
        {
            _logger.LogDebug("Circuit {CircuitId}: auth state not published yet, awaiting the change event", _circuitId);
        }
    }

    private void ApplyWhenReady(Task<AuthenticationState> stateTask)
    {
        _ = Apply();

        async Task Apply()
        {
            var circuitId = _circuitId;
            if (circuitId is null) return;

            try
            {
                var state = await stateTask;
                var login = state.User.Identity?.IsAuthenticated == true ? state.User.Identity.Name : null;

                _registry.SetLogin(circuitId, login);

                bool changed;
                lock (_applyGate)
                {
                    changed = !_everApplied || !string.Equals(_lastLogin, login, StringComparison.Ordinal);
                    _lastLogin = login;
                    _everApplied = true;
                }

                if (!changed) return;

                if (login is null)
                {
                    _logger.LogDebug("Circuit {CircuitId} is anonymous", circuitId);
                }
                else
                {
                    _logger.LogInformation("Circuit {CircuitId} attributed to login {Login}; connected logins now: {Logins}",
                        circuitId, login, string.Join(", ", _registry.ConnectedLogins()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Circuit {CircuitId}: could not resolve the login for rappel pop-ups", circuitId);
            }
        }
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        _authStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        _subscribed = false;
    }

    public void Dispose()
    {
        Unsubscribe();
        if (_circuitId is not null) _registry.CircuitClosed(_circuitId);
    }
}
