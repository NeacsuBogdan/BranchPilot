using BranchPilot.Domain.Utilities;

namespace BranchPilot.UnitTests.Domain;

public sealed class TenantSlugGeneratorTests
{
    [Fact]
    public void Generate_RemovesDiacriticsAndCollapsesSeparators()
    {
        var result = TenantSlugGenerator.Generate("Șantier & Café Central");

        Assert.Equal("santier-cafe-central", result);
    }

    [Fact]
    public void Generate_ReturnsTenant_WhenValueContainsNoAlphaNumericCharacters()
    {
        var result = TenantSlugGenerator.Generate("!!!");

        Assert.Equal("tenant", result);
    }
}
