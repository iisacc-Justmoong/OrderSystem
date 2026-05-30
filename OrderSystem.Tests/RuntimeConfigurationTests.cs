using System.Xml.Linq;
using Xunit;

namespace OrderSystem.Tests;

public sealed class RuntimeConfigurationTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void DirectoryBuildProps_AllowsMajorRuntimeRollForwardForLocalDotnetNineHosts()
    {
        var propsPath = Path.Combine(RepositoryRoot, "Directory.Build.props");
        var props = XDocument.Load(propsPath);

        var rollForward = props
            .Descendants("RollForward")
            .SingleOrDefault();

        Assert.NotNull(rollForward);
        Assert.Equal("Major", rollForward.Value);
    }
}
