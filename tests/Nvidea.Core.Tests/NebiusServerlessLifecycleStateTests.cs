using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusServerlessLifecycleStateTests
{
    [Theory]
    [InlineData("PROVISIONING")]
    [InlineData("IMAGE_PULLING")]
    [InlineData("STARTING")]
    public void ParseState_MapsPreparationStatesToPending(string rawState)
    {
        Assert.Equal(NebiusRemoteJobState.Pending, NebiusServerlessJobSnapshotParser.ParseState(rawState));
    }

    [Theory]
    [InlineData("CANCELLING")]
    [InlineData("DELETING")]
    public void ParseState_MapsTeardownStatesToCancelling(string rawState)
    {
        Assert.Equal(NebiusRemoteJobState.Cancelling, NebiusServerlessJobSnapshotParser.ParseState(rawState));
    }

    [Theory]
    [InlineData("NEW_FUTURE_STATE")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseState_LeavesUnknownFutureStatesFailClosed(string? rawState)
    {
        Assert.Equal(NebiusRemoteJobState.Unknown, NebiusServerlessJobSnapshotParser.ParseState(rawState));
    }

    [Fact]
    public void ParseState_IsWhitespaceAndCaseTolerantForDocumentedStates()
    {
        Assert.Equal(NebiusRemoteJobState.Pending, NebiusServerlessJobSnapshotParser.ParseState("  image_pulling  "));
        Assert.Equal(NebiusRemoteJobState.Cancelling, NebiusServerlessJobSnapshotParser.ParseState(" deleting "));
    }
}