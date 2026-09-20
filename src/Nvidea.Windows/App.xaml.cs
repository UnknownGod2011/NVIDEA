using System.IO;
using System.Windows;
using Nvidea.Core.Desktop;
using Nvidea.Core.Memory;

namespace Nvidea.Windows;

public partial class App : Application
{
    private NvideaCompositionRoot? _root;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            _root = await NvideaCompositionRoot.CreateFromEnvironmentAsync().ConfigureAwait(true);
            var window = new MainWindow(_root);
            MainWindow = window;
            window.Show();
        }
        catch (OperationCanceledException)
        {
            ShutdownAfterStartupCancellation();
        }
        catch (InvalidDataException)
        {
            try
            {
                if (await TryRecoverMemoryWithExplicitConsentAsync().ConfigureAwait(true))
                {
                    MessageBox.Show(
                        "The previous durable memory generation was restored successfully. NVIDEA will now close. Restart it to open the recovered state. No memory content was displayed during recovery.",
                        "Memory recovery complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Shutdown(0);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation during the independent recovery-eligibility probe must not
                // fall through to the generic configuration failure or authorize rollback.
                ShutdownAfterStartupCancellation();
                return;
            }

            MessageBox.Show(
                BuildCredentialSafeStartupFailureText(),
                "NVIDEA startup failed safely",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
        catch
        {
            MessageBox.Show(
                BuildCredentialSafeStartupFailureText(),
                "NVIDEA startup failed safely",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_root is not null)
            _root.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.OnExit(e);
    }

    private static async Task<bool> TryRecoverMemoryWithExplicitConsentAsync()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            return false;

        var memoryPath = Path.Combine(localAppData, "NVIDEA", "memory.json");
        if (!await MemoryRecoveryAvailabilityProbe.IsRecoveryOfferAllowedAsync(memoryPath).ConfigureAwait(true))
            return false;

        var choice = MessageBox.Show(
            "NVIDEA verified that the current personal-memory file cannot be opened safely, and a previous generation is available.\n\n" +
            "Recovering will replace the current personal-memory file with exactly one earlier last-known-good generation. Recent memory changes after that generation may be lost. Recovery does not inspect or display memory content, does not contact a cloud provider, and is never automatic.\n\n" +
            "Choose OK only if you want to perform this one-generation rollback now. Choose Cancel to leave every file unchanged.",
            "Recover previous memory generation?",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Cancel);

        if (choice != MessageBoxResult.OK)
            return false;

        using var store = new JsonFileMemoryStore(memoryPath);
        try
        {
            _ = await store.RecoverLastKnownGoodAsync().ConfigureAwait(true);
            return true;
        }
        catch (OperationCanceledException)
        {
            // Preserve cancellation semantics. The startup caller owns shutdown behavior;
            // cancellation must never be disguised as a failed recovery or trigger fallback.
            throw;
        }
        catch
        {
            MessageBox.Show(
                "Memory recovery failed safely. The previous generation could not be validated with this Windows protection context, so NVIDEA did not intentionally replace the current memory file. No memory content or protector error details are displayed.",
                "Memory recovery failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private void ShutdownAfterStartupCancellation()
    {
        // Startup/recovery cancellation is neither a credential/configuration failure nor
        // evidence that memory rollback is safe. Exit without offering recovery or emitting
        // misleading provider diagnostics. A later launch starts from durable state normally.
        Shutdown(0);
    }

    private static string BuildCredentialSafeStartupFailureText()
    {
        try
        {
            var cloudMode = DesktopResearchCloudMode.FromEnvironment();
            var readiness = DesktopResearchReadiness
                .InspectEnvironment(cloudMode)
                .WithRuntimeState(
                    lifecycleReady: false,
                    dispatchReady: false,
                    cloudPreflightFailed: cloudMode.LifecycleEnabled);

            return "NVIDEA could not start. No secret or provider exception text is displayed.\n\n" +
                   readiness.ToStatusText() +
                   "\n\nFix the named configuration blockers or run the Nebius contract probe for redacted deployment diagnostics, then restart.";
        }
        catch
        {
            return "NVIDEA could not start. No secret or provider exception text is displayed.\n\n" +
                   "Desktop research configuration is invalid or incomplete. Verify the documented NVIDEA_DESKTOP_REMOTE_RESEARCH_* flags and Nebius live configuration, run the Nebius contract probe for redacted diagnostics, then restart.";
        }
    }
}
