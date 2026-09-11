using System.Windows;
using Nvidea.Core.Jobs;

namespace Nvidea.Windows;

public partial class ResearchCloudDispatchDialog : Window
{
    public ResearchCloudDispatchDialog(ResearchJobStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.JobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(status));
        if (string.IsNullOrWhiteSpace(status.CheckpointStep))
            throw new ArgumentException("Research checkpoint step is required.", nameof(status));
        if (status.ContainsPrivateOsData)
            throw new InvalidOperationException("Private OS-local research cannot be approved for cloud dispatch.");

        InitializeComponent();
        JobIdText.Text = status.JobId.ToString("D");
        StageText.Text = status.Stage.ToString();
        CheckpointText.Text = status.CheckpointStep;
    }

    private void AcknowledgeCheck_Changed(object sender, RoutedEventArgs e)
    {
        ApproveButton.IsEnabled = AcknowledgeCheck.IsChecked == true;
    }

    private void Approve_Click(object sender, RoutedEventArgs e)
    {
        if (AcknowledgeCheck.IsChecked != true)
            return;

        DialogResult = true;
        Close();
    }

    private void Deny_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
