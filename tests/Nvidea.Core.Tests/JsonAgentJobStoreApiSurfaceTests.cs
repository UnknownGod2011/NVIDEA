using System.Reflection;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class JsonAgentJobStoreApiSurfaceTests
{
    [Fact]
    public void RawDurableJobStoreHasNoPublicConstructionPath()
    {
        Assert.Empty(typeof(JsonAgentJobStore).GetConstructors(BindingFlags.Instance | BindingFlags.Public));

        Assert.Contains(
            typeof(JsonAgentJobStore).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic),
            static constructor =>
            {
                var parameters = constructor.GetParameters();
                return constructor.IsAssembly
                    && parameters.Length == 2
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(ILocalStateProtector);
            });
    }

    [Fact]
    public void DurableStoreContractRemainsAvailableForLeastAuthorityComposition()
    {
        Assert.Contains(typeof(IAgentJobStore), typeof(JsonAgentJobStore).GetInterfaces());
        Assert.True(typeof(IAgentJobStore).IsPublic);
    }

    [Fact]
    public void RawStoreStillAdvertisesLowLevelMutationOnlyBehindNonPublicConstruction()
    {
        var save = typeof(JsonAgentJobStore).GetMethod(nameof(IAgentJobStore.SaveAsync), BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(save);
        Assert.Empty(typeof(JsonAgentJobStore).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
    }
}
