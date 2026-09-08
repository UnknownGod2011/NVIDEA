using System.Windows;
using Nvidea.Core.Browser;

namespace Nvidea.Windows;

public partial class DownloadHandoffDialog : Window
{
    public DownloadHandoffDialog(BrowserDownloadRecord record, BrowserDownloadHandoffPlan plan)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(plan);
        if (record.DownloadId != plan.DownloadId)
            throw new ArgumentException("Download record and handoff plan do not match.", nameof(plan));

        InitializeComponent();
        FileNameText.Text = record.SuggestedFileName;
        SourceText.Text = record.SourceUri.IdnHost;
        var length = record.LengthBytes is long bytes ? FormatBytes(bytes) : "unknown size";
        var hash = string.IsNullOrWhiteSpace(record.Sha256) ? "SHA-256 unavailable" : $"SHA-256 {record.Sha256}";
        VerifiedText.Text = $"{length} · {hash}";
        DestinationText.Text = plan.DestinationPath;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} {units[unit]}" : $"{value:0.##} {units[unit]} ({bytes:N0} bytes)";
    }
}
