using System.Text.Json;
using Nvidea.Core.Desktop;
using Xunit;

namespace Nvidea.DemoPackageValidator.Tests;

/// <summary>
/// Locks the checked-in judge manifest directly to the production recording contract.
/// This deliberately reads the repository artifact rather than a synthetic fixture so
/// documentation drift cannot silently create a demo sequence the Windows gate rejects.
/// </summary>
public sealed class DemoRecordingContractAlignmentTests
{
    [Fact]
    public void Checked_in_manifest_matches_production_recording_contract_exactly()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "demo-package.json")));

        var declared = document.RootElement.GetProperty("beats")
            .EnumerateArray()
            .SelectMany(beat => beat.GetProperty("expectedSessionMilestones").EnumerateArray())
            .Select(value => value.GetString() ?? throw new InvalidDataException("Demo milestone cannot be null."))
            .ToArray();

        var expected = DemoRecordingContract.RequiredSequence
            .Select(kind => kind.ToString())
            .ToArray();

        Assert.Equal(expected, declared);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "docs", "demo-package.json")) &&
                File.Exists(Path.Combine(current.FullName, "src", "Nvidea.Core", "Nvidea.Core.csproj")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NVIDEA repository root from the test output directory.");
    }
}
