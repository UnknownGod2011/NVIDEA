using System.Reflection;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class NvideaCompositionRootResearchApiSurfaceTests
{
    [Fact]
    public void CompositionRootExposesLifecycleAwareResearchButNotLocalResearchRuntime()
    {
        var publicMembers = typeof(NvideaCompositionRoot)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(
            publicMembers,
            static member => member is PropertyInfo property
                && property.Name == nameof(NvideaCompositionRoot.Research)
                && property.PropertyType == typeof(ResearchProductRuntime));

        Assert.DoesNotContain(
            publicMembers,
            static member => member switch
            {
                PropertyInfo property => property.PropertyType == typeof(ResearchJobRuntime),
                MethodInfo method => method.ReturnType == typeof(ResearchJobRuntime)
                    || method.GetParameters().Any(static parameter => parameter.ParameterType == typeof(ResearchJobRuntime)),
                _ => false
            });
    }
}
