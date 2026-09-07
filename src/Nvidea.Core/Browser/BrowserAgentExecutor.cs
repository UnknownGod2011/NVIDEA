namespace Nvidea.Core.Browser;

public sealed class BrowserAgentExecutor
{
    private readonly IBrowserDriver _driver;
    private readonly BrowserSafetyPolicy _safety;
    private readonly IBrowserApprovalGate _approval;
    private readonly IBrowserActionVerifier _verifier;
    private readonly IBrowserCapabilityGuard? _capabilityGuard;

    public BrowserAgentExecutor(
        IBrowserDriver driver,
        BrowserSafetyPolicy safety,
        IBrowserApprovalGate approval,
        IBrowserActionVerifier verifier,
        IBrowserCapabilityGuard? capabilityGuard = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _safety = safety ?? throw new ArgumentNullException(nameof(safety));
        _approval = approval ?? throw new ArgumentNullException(nameof(approval));
        _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
        _capabilityGuard = capabilityGuard;
    }

    public async Task<IReadOnlyList<BrowserActionReceipt>> ExecutePlanAsync(
        IReadOnlyList<BrowserAction> plan,
        int maxActions = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Count == 0)
            return Array.Empty<BrowserActionReceipt>();
        if (plan.Count > Math.Clamp(maxActions, 1, 50))
            throw new InvalidOperationException("Browser plan exceeds the configured action budget.");

        var receipts = new List<BrowserActionReceipt>(plan.Count);
        foreach (var action in plan)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var receipt = await ExecuteOneAsync(action, cancellationToken).ConfigureAwait(false);
            receipts.Add(receipt);
            if (!receipt.DriverReportedSuccess || !receipt.Verified)
                break;
        }

        return receipts;
    }

    public async Task<BrowserActionReceipt> ExecuteOneAsync(
        BrowserAction action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        var before = await _driver.ObserveAsync(cancellationToken).ConfigureAwait(false);
        var started = DateTimeOffset.UtcNow;
        var actionId = Guid.NewGuid();
        var decision = _safety.Evaluate(action, before);
        if (_capabilityGuard is not null)
            decision = _capabilityGuard.Evaluate(actionId, action, before, decision);

        if (!decision.Allowed)
        {
            return new BrowserActionReceipt(
                actionId, action, decision, started, DateTimeOffset.UtcNow,
                DriverReportedSuccess: false, Verified: false,
                VerificationDetail: "Blocked by browser/capability safety policy.",
                before.Url, before.Url, decision.Reason);
        }

        if (decision.RequiresApproval)
        {
            var approved = await _approval.RequestApprovalAsync(action, decision, before, cancellationToken).ConfigureAwait(false);
            if (!approved)
            {
                return new BrowserActionReceipt(
                    actionId, action, decision, started, DateTimeOffset.UtcNow,
                    DriverReportedSuccess: false, Verified: false,
                    VerificationDetail: "User approval was not granted.",
                    before.Url, before.Url, "Approval denied or unavailable.");
            }
        }

        try
        {
            await _driver.ExecuteAsync(action, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var after = await _driver.ObserveAsync(cancellationToken).ConfigureAwait(false);
            var verification = await _verifier.VerifyAsync(action, before, after, cancellationToken).ConfigureAwait(false);

            return new BrowserActionReceipt(
                actionId, action, decision, started, DateTimeOffset.UtcNow,
                DriverReportedSuccess: true, Verified: verification.Verified,
                VerificationDetail: verification.Detail,
                before.Url, after.Url);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            BrowserObservation? after = null;
            try { after = await _driver.ObserveAsync(CancellationToken.None).ConfigureAwait(false); } catch { }
            return new BrowserActionReceipt(
                actionId, action, decision, started, DateTimeOffset.UtcNow,
                DriverReportedSuccess: false, Verified: false,
                VerificationDetail: "Driver action failed before verification completed.",
                before.Url, after?.Url ?? before.Url, ex.Message);
        }
    }
}

public sealed class ConservativeBrowserVerifier : IBrowserActionVerifier
{
    public Task<(bool Verified, string Detail)> VerifyAsync(
        BrowserAction action,
        BrowserObservation before,
        BrowserObservation after,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (action.Kind == BrowserActionKind.Read)
            return Task.FromResult((true, "Read-only observation completed."));

        if (action.Postconditions is { Count: > 0 })
            return Task.FromResult(BrowserPostconditionEvaluator.VerifyAll(action.Postconditions, after));

        if (action.Kind == BrowserActionKind.Navigate && action.Destination is not null)
        {
            var result = BrowserPostconditionEvaluator.VerifyOne(
                new BrowserPostcondition(BrowserPostconditionKind.UrlEquals, action.Destination.AbsoluteUri), after);
            return Task.FromResult(result);
        }

        // Legacy persisted actions may still contain ExpectedState. Keep this path only for
        // backwards compatibility; new planner output should use typed postconditions.
        if (!string.IsNullOrWhiteSpace(action.ExpectedState))
        {
            var expected = action.ExpectedState.Trim();
            var verified = after.VisibleText.Contains(expected, StringComparison.OrdinalIgnoreCase)
                || after.Title.Contains(expected, StringComparison.OrdinalIgnoreCase)
                || after.Elements.Any(element =>
                    (element.Name?.Contains(expected, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (element.Value?.Contains(expected, StringComparison.OrdinalIgnoreCase) ?? false));
            return Task.FromResult((verified,
                verified ? "Legacy expected-state text observed." : "Legacy expected-state text was not observed."));
        }

        var changed = before.Url != after.Url
            || !string.Equals(before.Title, after.Title, StringComparison.Ordinal)
            || !string.Equals(before.SnapshotId, after.SnapshotId, StringComparison.Ordinal);

        return Task.FromResult((changed,
            changed ? "Browser state changed after action." : "No verifiable browser-state change was observed; action is treated as unverified."));
    }
}

public sealed class DenyByDefaultApprovalGate : IBrowserApprovalGate
{
    public Task<bool> RequestApprovalAsync(
        BrowserAction action,
        BrowserActionDecision decision,
        BrowserObservation observation,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }
}
