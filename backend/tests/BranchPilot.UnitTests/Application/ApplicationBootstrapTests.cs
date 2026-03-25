using BranchPilot.Application;
using Microsoft.Extensions.DependencyInjection;

namespace BranchPilot.UnitTests.Application;

public class ApplicationBootstrapTests
{
    [Fact]
    public void AddApplication_ReturnsTheProvidedServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
    }
}
