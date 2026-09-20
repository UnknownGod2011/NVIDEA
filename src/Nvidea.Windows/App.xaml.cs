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
        catch (InvalidDataException)
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
        if (!File.Exists($"{memoryPath}.bak"))
            return false;

        var choice = MessageBox.Show(
            "NVIDEA could not safely open durable local state and a previous personal-memory generation is available.\n\n" +
            "Recovering will replace the current personal-memory file with exactly one earlier last-known-good generation. Recent memory changes after that generation may be lost. Recovery does not inspect or display memory content, does not contact a cloud provider, and is never automatic.\n\n" +
            "Choose OK only if you want to perform this one-generation rollback now. Choose Cancel to leave every file unchanged.",
            "Recover previous memory generation?",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Cancel);

        if (choice != MessageBoxResult.OK)
            return false;

        try
        {
            using var store = new JsonFileMemoryStore(memoryPath);
            _ = await store.RecoverLastKnownGoodAsync().ConfigureAwait(true);
            return true;
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
