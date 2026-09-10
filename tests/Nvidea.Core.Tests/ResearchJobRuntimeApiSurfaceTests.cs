using System.Reflection;
using Nvidea.Core.Jobs;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchJobRuntimeApiSurfaceTests
{
    [Fact]
    public void ConcreteLocalResearchRuntimeHasNoPublicConstructionPath()
    {
        Assert.Empty(typeof(ResearchJobRuntime).GetConstructors(BindingFlags.Instance | BindingFlags.Public));

        Assert.Contains(
            typeof(ResearchJobRuntime).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic),
            static constructor =>
            {
                var parameters = constructor.GetParameters();
                return constructor.IsAssembly
                    && parameters.Length == 3
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(ResearchEngine);
            });
    }

    [Fact]
    public void LeastAuthorityLocalResearchContractRemainsPubliclyUsable()
    {
        Assert.Contains(typeof(ILocalResearchRuntime), typeof(ResearchJobRuntime).GetInterfaces());
        Assert.True(typeof(ILocalResearchRuntime).IsPublic);
    }
}
