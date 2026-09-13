using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Tests;

public sealed class BrowserDiagnosticPrivacyTests
{
    private const string Secret = "Bearer super-secret-browser-token";
    private const string CapabilityId = "browser.agent";

    [Fact]
    public async Task CapabilityExecution_PostActionObservationFailure_DoesNotExposeRawDiagnostic()
    {
        var observation = Observation();
        var driver = new ThrowingObservationDriver(observation, Secret);
        var descriptor = new CapabilityDescriptor(
            CapabilityId,
            "1.0.0",
            "Browser agent",
            new HashSet<DataPermission> { DataPermission.BrowserRead },
            CapabilityRiskLevel.Low,
            RequiresConfirmation: false,
            "Permissioned browser interaction.");
        var registry = new CapabilityRegistry(new[] { descriptor });
        var policy = new CapabilityPermissionPolicy(registry);
        var service = new BrowserCapabilityExecutionService(
            CapabilityId,
            driver,
            new BrowserSafetyPolicy(),
            policy,
            new CapabilityToolExecutor(
                policy,
                new ScopedApprovalAuthorizer(),
                new InMemoryAuditTrail(),
                new BrowserCapabilityBackend(driver)),
            new ConservativeBrowserVerifier());

        var result = await service.ExecuteAsync(new BrowserAction(BrowserActionKind.Read));

        Assert.True(result.Receipt.DriverReportedSuccess);
        Assert.False(result.Receipt.Verified);
        Assert.DoesNotContain(Secret, result.Receipt.Error ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, result.Receipt.VerificationDetail ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("diagnostics are quarantined", result.Receipt.VerificationDetail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacyExecutor_DriverFailure_DoesNotExposeRawDiagnostic()
    {
        var observation = Observation();
        var driver = new ThrowingExecutionDriver(observation, Secret);
        var executor = new BrowserAgentExecutor(
            driver,
            new BrowserSafetyPolicy(),
            new AlwaysApproveGate(),
            new ConservativeBrowserVerifier());

        var receipt = await executor.ExecuteOneAsync(new BrowserAction(BrowserActionKind.Read));

        Assert.False(receipt.DriverReportedSuccess);
        Assert.False(receipt.Verified);
        Assert.DoesNotContain(Secret, receipt.Error ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, receipt.VerificationDetail ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("diagnostics are quarantined", receipt.VerificationDetail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static BrowserObservation Observation() => new(
        new Uri("https://example.test/"),
        "Example",
        Array.Empty<BrowserElement>(),
        "Safe page",
        DateTimeOffset.UtcNow,
        SnapshotId: "before");

    private sealed class ThrowingObservationDriver : IBrowserDriver
    {
        private readonly BrowserObservation _observation;
        private readonly string _diagnostic;
        private int _observations;

        public ThrowingObservationDriver(BrowserObservation observation, string diagnostic)
        {
            _observation = observation;
            _diagnostic = diagnostic;
        }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _observations++;
            if (_observations > 1)
                throw new InvalidOperationException(_diagnostic);
            return Task.FromResult(_observation);
        }

        public Task ExecuteAsync(BrowserAction action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingExecutionDriver : IBrowserDriver
    {
        private readonly BrowserObservation _observation;
        private readonly string _diagnostic;

        public ThrowingExecutionDriver(BrowserObservation observation, string diagnostic)
        {
            _observation = observation;
            _diagnostic = diagnostic;
        }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_observation);
        }

        public Task ExecuteAsync(BrowserAction action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException(_diagnostic);
        }
    }

    private sealed class AlwaysApproveGate : IBrowserApprovalGate
    {
        public Task<bool> RequestApprovalAsync(
            BrowserAction action,
            BrowserActionDecision decision,
            BrowserObservation observation,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }
    }

    private sealed class InMemoryAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<AuditEvent>>(Events.ToArray());
        }
    }
}
