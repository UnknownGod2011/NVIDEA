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
        catch (Exception ex)
        {
            MessageBox.Show(
                $"NVIDEA could not start.\n\n{ex.Message}\n\nSet the required Nebius environment variables and restart.",
                "NVIDEA startup failed",
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
}
