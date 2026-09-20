using System.Text.Json;
using Nvidea.Core.Browser;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Bridges authoritative durable browser-job completion into the protected judge-evidence store.
/// The bridge accepts only the least-authority structural projection embedded by
/// <see cref="BrowserActionJobHandler"/> and never reconstructs browser payloads or approval authority.
/// </summary>
internal sealed class DurableBrowserVerificationPublisher
{
    private const string VerifiedCheckpointStep = "browser.action.verified";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DurableBrowserVerificationReceiptStore _store;

    public DurableBrowserVerificationPublisher(DurableBrowserVerificationReceiptStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Clears the previous completed-action receipt before a new action is durably admitted. A clear
    /// failure must abort admission so stale evidence can never be presented as proof of the new run.
    /// </summary>
    public Task BeginActionAsync(CancellationToken cancellationToken = default) =>
        _store.ClearAsync(cancellationToken);

    /// <summary>
    /// Publishes evidence only from an orchestrator-returned Completed record. Legacy/reconciled
    /// checkpoints without structural evidence return false and never overwrite the store.
    /// Malformed structural evidence fails closed by throwing before persistence.
    /// </summary>
    public async Task<bool> TryPublishCompletedAsync(
        AgentJobRecord job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.State != AgentJobState.Completed
            || job.Checkpoint is null
            || !string.Equals(job.Checkpoint.Step, VerifiedCheckpointStep, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(job.Checkpoint.Payload))
        {
            return false;
        }

        DurableBrowserActionEvidence? evidence;
        try
        {
            using var document = JsonDocument.Parse(job.Checkpoint.Payload);
            if (!document.RootElement.TryGetProperty("durableEvidence", out var evidenceElement)
                || evidenceElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return false;
            }

            evidence = evidenceElement.Deserialize<DurableBrowserActionEvidence>(JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Completed browser checkpoint contains malformed durable evidence.", ex);
        }

        if (evidence is null)
            throw new InvalidDataException("Completed browser checkpoint durable evidence is empty.");
        if (evidence.ActionId != job.JobId)
            throw new InvalidDataException("Completed browser checkpoint evidence does not belong to this durable job.");
        if (!evidence.Allowed || !evidence.DriverReportedSuccess || !evidence.PostStateVerified)
            throw new InvalidDataException("Completed browser checkpoint does not contain verified successful execution evidence.");
        if (evidence.RequiredApproval && !evidence.ApprovalObserved)
            throw new InvalidDataException("Consequential browser completion is missing historical approval evidence.");

        var receipt = DurableBrowserVerificationReceipt.CreateFromEvidence(new[] { evidence });
        await _store.WriteAsync(receipt, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<DesktopBrowserVerificationPresentation> ReadPresentationAsync(
        CancellationToken cancellationToken = default)
    {
        var receipt = await _store.ReadAsync(cancellationToken).ConfigureAwait(false);
        return receipt?.ToPresentation()
            ?? DesktopBrowserVerificationProjector.NotVerifiedDurable("No durable browser verification evidence is available.");
    }
}
