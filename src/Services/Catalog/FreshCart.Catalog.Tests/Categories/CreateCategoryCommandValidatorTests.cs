using FluentValidation.TestHelper;
using FreshCart.Catalog.Api.Features.Categories;
using FreshCart.Catalog.Api.Features.Categories.CreateCategory;

namespace FreshCart.Catalog.Tests.Categories;

public sealed class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator validator = new();

    [Fact]
    public void AcceptsAValidRootCategory()
    {
        validator.TestValidate(new CreateCategoryCommand("Online Courses", "Video courses.", null, 6))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AcceptsAValidChildCategory()
    {
        validator.TestValidate(new CreateCategoryCommand("Developer Tools", null, Guid.NewGuid(), 1))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsMissingName(string name)
    {
        validator.TestValidate(new CreateCategoryCommand(name, null, null, 1))
            .ShouldHaveValidationErrorFor(invalid => invalid.Name);
    }

    [Fact]
    public void RejectsNameLongerThanTheLimit()
    {
        validator.TestValidate(new CreateCategoryCommand(new string('a', CategoryConstraints.MaxNameLength + 1), null, null, 1))
            .ShouldHaveValidationErrorFor(invalid => invalid.Name);
    }

    [Fact]
    public void RejectsDescriptionLongerThanTheLimit()
    {
        var oversizedDescription = new string('a', CategoryConstraints.MaxDescriptionLength + 1);

        validator.TestValidate(new CreateCategoryCommand("Online Courses", oversizedDescription, null, 1))
            .ShouldHaveValidationErrorFor(invalid => invalid.Description);
    }

    [Fact]
    public void RejectsAnEmptyParentIdentifierBecauseItMeansACallerBugNotARootCategory()
    {
        validator.TestValidate(new CreateCategoryCommand("Online Courses", null, Guid.Empty, 1))
            .ShouldHaveValidationErrorFor(invalid => invalid.ParentCategoryId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(CategoryConstraints.MaxSortOrder + 1)]
    public void RejectsSortOrderOutsideTheAllowedRange(int sortOrder)
    {
        validator.TestValidate(new CreateCategoryCommand("Online Courses", null, null, sortOrder))
            .ShouldHaveValidationErrorFor(invalid => invalid.SortOrder);
    }
}
