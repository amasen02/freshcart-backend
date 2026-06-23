using FluentAssertions;
using FreshCart.Catalog.Api.Slugs;

namespace FreshCart.Catalog.Tests.Slugs;

public sealed class SlugGeneratorTests
{
    [Theory]
    [InlineData("Online Courses", "online-courses")]
    [InlineData("TaskFlow Pro 2026", "taskflow-pro-2026")]
    [InlineData("E-Books", "e-books")]
    [InlineData("Lo-Fi Study Sessions Vol. 3", "lo-fi-study-sessions-vol-3")]
    public void LowercasesAndHyphenatesDisplayNames(string sourceText, string expectedSlug)
    {
        SlugGenerator.Generate(sourceText).Should().Be(expectedSlug);
    }

    [Fact]
    public void CollapsesRunsOfNonAlphanumericCharactersIntoASingleHyphen()
    {
        SlugGenerator.Generate("Cloud   &   Edge -- Computing!").Should().Be("cloud-edge-computing");
    }

    [Fact]
    public void TrimsLeadingAndTrailingSeparators()
    {
        SlugGenerator.Generate("  ...Symphony No. 9...  ").Should().Be("symphony-no-9");
    }

    [Fact]
    public void TruncatesVeryLongNamesToTheMaximumSlugLength()
    {
        var veryLongName = string.Join(' ', Enumerable.Repeat("word", 60));

        var slug = SlugGenerator.Generate(veryLongName);

        slug.Length.Should().BeLessThanOrEqualTo(120);
        slug.Should().NotEndWith("-");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsBlankInput(string sourceText)
    {
        var generating = () => SlugGenerator.Generate(sourceText);

        generating.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RejectsInputWithNoUsableCharacters()
    {
        var generating = () => SlugGenerator.Generate("!!! ***");

        generating.Should().Throw<ArgumentException>();
    }
}
