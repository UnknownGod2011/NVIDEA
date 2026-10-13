using System.Xml.Linq;
using Xunit;

namespace Nvidea.DemoPackageValidator.Tests;

/// <summary>
/// Guards the Windows judge evidence wiring without executing WPF or requiring a Nebius account.
/// Cryptographic worker origin is verified elsewhere; this panel must never imply that merely
/// configuring a local public key proves a deployed worker actually signed a result.
/// </summary>
public sealed class WorkerAuthenticationJudgeWiringTests
{
    [Fact]
    public void Judge_dialog_exposes_a_single_worker_identity_panel_and_fails_closed()
    {
        var root = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "Nvidea.Windows", "JudgeEvidenceDialog.xaml"));
        var dialog = File.ReadAllText(Path.Combine(root, "src", "Nvidea.Windows", "JudgeEvidenceDialog.xaml.cs"));
        var readiness = File.ReadAllText(Path.Combine(root, "src", "Nvidea.Windows", "MainWindow.Readiness.cs"));

        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var controls = XDocument.Parse(xaml).Descendants()
            .Where(node => (string?)node.Attribute(x + "Name") == "WorkerAuthenticationText")
            .ToArray();

        Assert.Single(controls);
        Assert.Contains("Remote worker authentication", xaml, StringComparison.Ordinal);
        Assert.Contains("WorkerAuthenticationText.Text = BuildWorkerAuthenticationSummary(workerAuthentication)", dialog, StringComparison.Ordinal);
        Assert.Contains("evidence is null", dialog, StringComparison.Ordinal);
        Assert.Contains("NOT VERIFIED", dialog, StringComparison.Ordinal);
        Assert.Contains("deployed worker identity NOT VERIFIED", dialog, StringComparison.Ordinal);
        Assert.Contains("WorkerAuthenticationJudgeEvidence.FromPinnedEnvironment()", readiness, StringComparison.Ordinal);
        Assert.Contains("researchProvenance, workerAuthentication)", readiness, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.GetEnvironmentVariable", dialog, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "progress.md")) &&
                File.Exists(Path.Combine(directory.FullName, "src", "Nvidea.Windows", "JudgeEvidenceDialog.xaml")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("NVIDEA repository root could not be found for Windows wiring contract.");
    }
}
