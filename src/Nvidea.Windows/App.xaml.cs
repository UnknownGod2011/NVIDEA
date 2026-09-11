using System.Windows;
using Nvidea.Core.Desktop;

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
