using BranchPilot.Domain.Enums;

namespace BranchPilot.Application.Catalog;

public static class CatalogItemTypeParser
{
    public static bool TryParse(string? value, out CatalogItemType itemType)
    {
        if (Enum.TryParse<CatalogItemType>(value?.Trim(), true, out itemType))
        {
            return true;
        }

        itemType = default;
        return false;
    }
}
