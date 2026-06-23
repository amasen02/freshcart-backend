using FluentValidation;

namespace FreshCart.Catalog.Api.Features.Categories.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(CategoryConstraints.MaxNameLength);

        RuleFor(command => command.Description)
            .MaximumLength(CategoryConstraints.MaxDescriptionLength);

        RuleFor(command => command.ParentCategoryId)
            .Must(parentCategoryId => parentCategoryId is null || parentCategoryId != Guid.Empty)
            .WithMessage("ParentCategoryId must be a non-empty identifier when provided.");

        RuleFor(command => command.SortOrder)
            .InclusiveBetween(0, CategoryConstraints.MaxSortOrder);
    }
}
