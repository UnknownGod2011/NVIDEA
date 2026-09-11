using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class LocalVoiceTranscriptionTests
{
    [Fact]
    public void PreparePrompt_TrimsRecognizedText()
    {
        var transcript = new LocalVoiceTranscript("  open my project notes  ", 0.82f, "en-US");

        Assert.Equal("open my project notes", transcript.PreparePrompt());
    }

    [Fact]
    public void PreparePrompt_RejectsBlankTranscript()
    {
        var transcript = new LocalVoiceTranscript("   ", 0.8f, "en-US");

        var exception = Assert.Throws<InvalidOperationException>(() => transcript.PreparePrompt());

        Assert.Equal("No speech was recognized.", exception.Message);
    }

    [Fact]
    public void PreparePrompt_RejectsOversizedTranscriptInsteadOfSilentlyTruncating()
    {
        var transcript = new LocalVoiceTranscript(new string('a', 11), 0.8f, "en-US");

        var exception = Assert.Throws<InvalidOperationException>(() => transcript.PreparePrompt(10));

        Assert.Contains("10-character review limit", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(-0.01f)]
    [InlineData(1.01f)]
    public void PreparePrompt_RejectsInvalidConfidence(float confidence)
    {
        var transcript = new LocalVoiceTranscript("hello", confidence, "en-US");

        Assert.Throws<InvalidOperationException>(() => transcript.PreparePrompt());
    }
}
