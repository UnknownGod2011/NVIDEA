using System.Windows;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class ApprovalDialog : Window
{
    public ApprovalDialog(BrowserApprovalPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        InitializeComponent();
        ActionText.Text = $"{prompt.ActionKind}: {prompt.Summary}";
        TargetText.Text = prompt.Target;
        ScopeText.Text = prompt.ExactScope;
    }

    private void Approve_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Deny_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
