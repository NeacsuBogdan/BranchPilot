using FluentValidation;
using BranchPilot.Domain.Enums;

namespace BranchPilot.Application.Catalog;

public sealed class UpsertCatalogItemRequestValidator : AbstractValidator<UpsertCatalogItemRequest>
{
    public UpsertCatalogItemRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(request => request.Code)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[A-Za-z0-9-]+$");

        RuleFor(request => request.ItemType)
            .Must(value => CatalogItemTypeParser.TryParse(value, out _))
            .WithMessage("The selected item type is not supported.");

        RuleFor(request => request.TaxProfileId)
            .NotEmpty();

        RuleFor(request => request.Description)
            .MaximumLength(1000);

        RuleFor(request => request.LocationPrices)
            .NotEmpty();

        RuleForEach(request => request.LocationPrices)
            .ChildRules(
                locationPrice =>
                {
                    locationPrice.RuleFor(item => item.LocationId)
                        .NotEmpty();

                    locationPrice.RuleFor(item => item.PriceAmount)
                        .GreaterThan(0m);

                    locationPrice.RuleFor(item => item.CurrencyCode)
                        .NotEmpty()
                        .Length(3)
                        .Matches("^[A-Za-z]{3}$");
                });

        RuleForEach(request => request.Promotions)
            .ChildRules(
                promotion =>
                {
                    promotion.RuleFor(item => item.Name)
                        .NotEmpty()
                        .MaximumLength(120);

                    promotion.RuleFor(item => item.LocationId)
                        .NotEmpty();

                    promotion.RuleFor(item => item.DiscountPercentage)
                        .GreaterThan(0m)
                        .LessThanOrEqualTo(100m);

                    promotion.RuleFor(item => item.EndsAtUtc)
                        .GreaterThan(item => item.StartsAtUtc);
                });

        When(
            request => CatalogItemTypeParser.TryParse(request.ItemType, out var itemType) && itemType == CatalogItemType.Service,
            () =>
            {
                RuleFor(request => request.DurationInMinutes)
                    .NotNull()
                    .InclusiveBetween(5, 480);
            });

        When(
            request => CatalogItemTypeParser.TryParse(request.ItemType, out var itemType) && itemType == CatalogItemType.Product,
            () =>
            {
                RuleFor(request => request.DurationInMinutes)
                    .Null();
            });
    }
}
