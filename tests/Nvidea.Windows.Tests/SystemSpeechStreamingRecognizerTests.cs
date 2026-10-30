using Nvidea.Windows;

namespace Nvidea.Windows.Tests;

public sealed class SystemSpeechStreamingRecognizerTests
{
    [Fact]
    public void Hypothesis_only_turn_finishes_after_pause_and_preserves_its_text()
    {
        var turn = new LocalSpeechTurnAccumulator();
        var spokenAt = DateTimeOffset.Parse("2026-10-30T12:00:00Z");
        turn.SetHypothesis("  open my project folder  ", 0.74f, spokenAt);

        Assert.False(turn.ShouldFinish(spokenAt.AddMilliseconds(899), TimeSpan.FromMilliseconds(900)));
        Assert.True(turn.ShouldFinish(spokenAt.AddMilliseconds(900), TimeSpan.FromMilliseconds(900)));
        Assert.True(turn.HasTranscript);
        Assert.Equal("open my project folder", turn.GetTranscript());
        Assert.Equal(0.74f, turn.Confidence);
    }

    [Fact]
    public void Committed_recognition_replaces_partial_hypothesis()
    {
        var turn = new LocalSpeechTurnAccumulator();
        var spokenAt = DateTimeOffset.Parse("2026-10-30T12:00:00Z");
        turn.SetHypothesis("open my project", 0.41f, spokenAt);
        turn.Commit("Open my project folder.", 0.93f, spokenAt.AddMilliseconds(400));

        Assert.Equal("Open my project folder.", turn.GetTranscript());
        Assert.Equal(0.93f, turn.Confidence);
        Assert.False(turn.ShouldFinish(spokenAt.AddMilliseconds(1_299), TimeSpan.FromMilliseconds(900)));
        Assert.True(turn.ShouldFinish(spokenAt.AddMilliseconds(1_300), TimeSpan.FromMilliseconds(900)));
    }

    [Fact]
    public void Multi_phrase_turn_reports_its_lowest_committed_recognition_confidence()
    {
        var turn = new LocalSpeechTurnAccumulator();
        var spokenAt = DateTimeOffset.Parse("2026-10-30T12:00:00Z");
        turn.Commit("Open the browser.", 0.96f, spokenAt);
        turn.Commit("Search for the project.", 0.79f, spokenAt.AddSeconds(1));

        Assert.Equal(0.79f, turn.Confidence);
    }

    [Fact]
    public void Empty_turn_never_finishes_or_claims_transcript()
    {
        var turn = new LocalSpeechTurnAccumulator();

        Assert.False(turn.ShouldFinish(DateTimeOffset.UtcNow.AddMinutes(1), TimeSpan.FromMilliseconds(900)));
        Assert.False(turn.HasTranscript);
        Assert.Equal(string.Empty, turn.GetTranscript());
        Assert.Equal(0f, turn.Confidence);
    }
}
