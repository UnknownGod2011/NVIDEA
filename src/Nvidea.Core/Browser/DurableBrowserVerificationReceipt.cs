using System.Security.Cryptography;
using System.Text;

namespace Nvidea.Core.Browser;

/// <summary>
/// Restart-stable, non-authorizing evidence for one completed browser action. This record is safe
/// to persist because it contains no URL, locator, typed value, page content, approval scope/token,
/// verification detail, provider diagnostic, or reusable authorization material.
/// ApprovalObserved is historical evidence only and MUST NEVER be accepted as authorization.
/// </summary>
public sealed record DurableBrowserActionEvidence(
    Guid ActionId,
    BrowserActionKind ActionKind,
    BrowserRiskLevel Risk,
    bool Allowed,
    bool RequiredApproval,
    bool ApprovalObserved,
    bool DriverReportedSuccess,
    bool PostStateVerified,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt);

/// <summary>
/// Payload-free receipt for a completed browser run. Commitment binds the ordered structural
/// evidence so accidental/tampered field changes fail closed when read back. It is an integrity
/// commitment, not a signature or third-party attestation.
/// </summary>
public sealed record DurableBrowserVerificationReceipt(
    int Version,
    DateTimeOffset CompletedAt,
    IReadOnlyList<DurableBrowserActionEvidence> Actions,
    string Commitment)
{
    public const int CurrentVersion = 1;

    public static DurableBrowserVerificationReceipt Create(IReadOnlyList<BrowserActionReceipt> receipts)
    {
        ArgumentNullException.ThrowIfNull(receipts);
        if (receipts.Count == 0)
            throw new ArgumentException("At least one browser receipt is required.", nameof(receipts));

        var actions = receipts.Select(Project).ToArray();
        ValidateActions(actions);
        var completedAt = actions.Max(static x => x.CompletedAt);
        return new DurableBrowserVerificationReceipt(
            CurrentVersion,
            completedAt,
            actions,
            ComputeCommitment(CurrentVersion, completedAt, actions));
    }

    public bool HasValidIntegrity()
    {
        if (Version != CurrentVersion || Actions is null || Actions.Count == 0 || string.IsNullOrWhiteSpace(Commitment))
            return false;

        try
        {
            ValidateActions(Actions);
            if (CompletedAt != Actions.Max(static x => x.CompletedAt))
                return false;
            var expected = ComputeCommitment(Version, CompletedAt, Actions);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expected),
                Encoding.ASCII.GetBytes(Commitment.Trim().ToLowerInvariant()));
        }
        catch
        {
            return false;
        }
    }

    public DesktopBrowserVerificationPresentation ToPresentation()
    {
        if (!HasValidIntegrity())
            return DesktopBrowserVerificationProjector.NotVerifiedDurable("Durable browser evidence failed integrity validation.");

        return DesktopBrowserVerificationProjector.ProjectDurable(Actions);
    }

    private static DurableBrowserActionEvidence Project(BrowserActionReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return new DurableBrowserActionEvidence(
            receipt.ActionId,
            receipt.Action.Kind,
            receipt.Decision.Risk,
            receipt.Decision.Allowed,
            receipt.Decision.RequiresApproval,
            receipt.ApprovalGranted,
            receipt.DriverReportedSuccess,
            receipt.Verified,
            receipt.StartedAt.ToUniversalTime(),
            receipt.CompletedAt.ToUniversalTime());
    }

    private static void ValidateActions(IReadOnlyList<DurableBrowserActionEvidence> actions)
    {
        var ids = new HashSet<Guid>();
        foreach (var action in actions)
        {
            if (action is null || action.ActionId == Guid.Empty || !ids.Add(action.ActionId))
                throw new InvalidDataException("Durable browser evidence contains an invalid or duplicate action identity.");
            if (action.CompletedAt < action.StartedAt)
                throw new InvalidDataException("Durable browser evidence contains an invalid timestamp range.");
            if (action.ApprovalObserved && !action.RequiredApproval)
                throw new InvalidDataException("Approval evidence cannot exist for an action that did not require approval.");
        }
    }

    private static string ComputeCommitment(
        int version,
        DateTimeOffset completedAt,
        IReadOnlyList<DurableBrowserActionEvidence> actions)
    {
        var canonical = new StringBuilder()
            .Append("nvidea-browser-verification\n")
            .Append(version).Append('\n')
            .Append(completedAt.ToUniversalTime().ToString("O")).Append('\n');

        foreach (var action in actions)
        {
            canonical.Append(action.ActionId.ToString("D")).Append('|')
                .Append((int)action.ActionKind).Append('|')
                .Append((int)action.Risk).Append('|')
                .Append(action.Allowed ? '1' : '0').Append('|')
                .Append(action.RequiredApproval ? '1' : '0').Append('|')
                .Append(action.ApprovalObserved ? '1' : '0').Append('|')
                .Append(action.DriverReportedSuccess ? '1' : '0').Append('|')
                .Append(action.PostStateVerified ? '1' : '0').Append('|')
                .Append(action.StartedAt.ToUniversalTime().ToString("O")).Append('|')
                .Append(action.CompletedAt.ToUniversalTime().ToString("O")).Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }
}
