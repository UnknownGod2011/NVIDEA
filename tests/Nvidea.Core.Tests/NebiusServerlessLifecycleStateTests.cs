using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusServerlessLifecycleStateTests
{
    [Theory]
    [InlineData("PROVISIONING", NebiusRemoteJobState.Pending)]
    [InlineData("STARTING", NebiusRemoteJobState.Pending)]
    [InlineData("IMAGE_PULLING", NebiusRemoteJobState.Pending)]
    [InlineData("RUNNING", NebiusRemoteJobState.Running)]
    [InlineData("COMPLETED", NebiusRemoteJobState.Completed)]
    [InlineData("CANCELLING", NebiusRemoteJobState.Cancelling)]
    [InlineData("CANCELLED", NebiusRemoteJobState.Cancelled)]
    [InlineData("DELETING", NebiusRemoteJobState.Cancelling)]
    [InlineData("FAILED", NebiusRemoteJobState.Failed)]
    [InlineData("ERROR", NebiusRemoteJobState.Failed)]
    [InlineData("STATE_UNSPECIFIED", NebiusRemoteJobState.Unknown)]
    public void ParseState_MapsEveryDocumentedNebiusJobState(
        string rawState,
        NebiusRemoteJobState expected)
    {
        Assert.Equal(expected, NebiusServerlessJobSnapshotParser.ParseState(rawState));
    }

    [Theory]
    [InlineData("NEW_FUTURE_STATE")]
    [InlineData("PENDING")]
    [InlineData("SUCCESS")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseState_LeavesUndocumentedOrMissingStatesFailClosed(string? rawState)
    {
        Assert.Equal(NebiusRemoteJobState.Unknown, NebiusServerlessJobSnapshotParser.ParseState(rawState));
    }

    [Theory]
    [InlineData("  provisioning  ", NebiusRemoteJobState.Pending)]
    [InlineData(" image_pulling ", NebiusRemoteJobState.Pending)]
    [InlineData(" running ", NebiusRemoteJobState.Running)]
    [InlineData(" completed ", NebiusRemoteJobState.Completed)]
    [InlineData(" cancelling ", NebiusRemoteJobState.Cancelling)]
    [InlineData(" cancelled ", NebiusRemoteJobState.Cancelled)]
    [InlineData(" deleting ", NebiusRemoteJobState.Cancelling)]
    [InlineData(" failed ", NebiusRemoteJobState.Failed)]
    [InlineData(" error ", NebiusRemoteJobState.Failed)]
    [InlineData(" state_unspecified ", NebiusRemoteJobState.Unknown)]
    public void ParseState_IsWhitespaceAndCaseTolerantOnlyForDocumentedStates(
        string rawState,
        NebiusRemoteJobState expected)
    {
        Assert.Equal(expected, NebiusServerlessJobSnapshotParser.ParseState(rawState));
    }
}