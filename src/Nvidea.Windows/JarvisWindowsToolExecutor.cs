using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

/// <summary>
/// The Windows boundary for Jarvis' small initial tool set. It accepts only schema-validated
/// tool arguments and requires a visible approval before each local side effect.
/// </summary>
internal sealed class JarvisWindowsToolExecutor(Window owner, WindowsFocusTarget? capturedTarget) : IJarvisToolExecutor
{
    private static readonly IReadOnlyDictionary<string, string> ApprovedApplications =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = "chrome.exe",
            ["google chrome"] = "chrome.exe",
            ["edge"] = "msedge.exe",
            ["microsoft edge"] = "msedge.exe",
            ["visual studio code"] = "code.exe",
            ["vs code"] = "code.exe",
            ["notepad"] = "notepad.exe",
            ["calculator"] = "calc.exe",
            ["file explorer"] = "explorer.exe"
        };

    public async Task<JarvisToolExecution> ExecuteAsync(string name, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return name switch
        {
            "open_application" => OpenApplication(GetBoundedString(arguments, "application", 120)),
            "open_website" => OpenWebsite(GetBoundedString(arguments, "url", 2048)),
            "type_text" => await TypeTextAsync(GetBoundedString(arguments, "text", 4_000), cancellationToken).ConfigureAwait(true),
            _ => new JarvisToolExecution(name, false, "Jarvis rejected an unsupported local capability.")
        };
    }

    private JarvisToolExecution OpenApplication(string application)
    {
        if (!ApprovedApplications.TryGetValue(application.Trim(), out var executable))
            return new JarvisToolExecution("open_application", false, "That application is not in Jarvis' approved local launch list.");
        if (!Approve($"Open {application.Trim()}?"))
            return new JarvisToolExecution("open_application", false, "The application launch was not approved.");
        try
        {
            _ = Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            return new JarvisToolExecution("open_application", true, $"Requested Windows to open {application.Trim()}.");
        }
        catch
        {
            return new JarvisToolExecution("open_application", false, $"Windows could not start {application.Trim()}.");
        }
    }

    private JarvisToolExecution OpenWebsite(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            return new JarvisToolExecution("open_website", false, "Jarvis rejected a website that was not a safe HTTPS address.");
        if (!Approve($"Open {uri.Host} in your default browser?"))
            return new JarvisToolExecution("open_website", false, "The browser launch was not approved.");
        try
        {
            _ = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            return new JarvisToolExecution("open_website", true, $"Requested the default browser to open {uri.Host}.");
        }
        catch
        {
            return new JarvisToolExecution("open_website", false, "Windows could not open the requested website.");
        }
    }

    private async Task<JarvisToolExecution> TypeTextAsync(string text, CancellationToken cancellationToken)
    {
        if (capturedTarget is null)
            return new JarvisToolExecution("type_text", false, "Jarvis has no safely captured target application for text insertion.");
        if (!Approve("Insert the prepared text into the application you invoked Jarvis from?"))
            return new JarvisToolExecution("type_text", false, "Text insertion was not approved.");
        try
        {
            await WindowsTextInsertion.InsertIntoCapturedTargetAsync(capturedTarget, text, cancellationToken).ConfigureAwait(true);
            return new JarvisToolExecution("type_text", true, "Inserted the approved text into the captured application.");
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return new JarvisToolExecution("type_text", false, "Text was not inserted because the captured application could not be safely restored.");
        }
    }

    private bool Approve(string summary) => MessageBox.Show(owner, summary, "Jarvis approval", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static string GetBoundedString(JsonElement arguments, string property, int maximumLength)
    {
        if (!arguments.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
            return string.Empty;
        var text = value.GetString()?.Trim() ?? string.Empty;
        return text.Length <= maximumLength ? text : string.Empty;
    }
}
