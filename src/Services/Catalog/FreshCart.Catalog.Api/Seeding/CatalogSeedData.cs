using System.Globalization;
using FreshCart.Catalog.Api.Models;
using FreshCart.Catalog.Api.Slugs;

namespace FreshCart.Catalog.Api.Seeding;

/// <summary>
/// Deterministic development catalog: fixed identifiers, fixed timestamps and no randomness, so
/// end-to-end tests can navigate to a known slug or sku and assert on exact prices and stock.
/// </summary>
public static class CatalogSeedData
{
    public static readonly Guid SoftwareCategoryId = new("11111111-1111-1111-1111-000000000001");
    public static readonly Guid GamesCategoryId = new("11111111-1111-1111-1111-000000000002");
    public static readonly Guid EBooksCategoryId = new("11111111-1111-1111-1111-000000000003");
    public static readonly Guid MusicCategoryId = new("11111111-1111-1111-1111-000000000004");
    public static readonly Guid GiftCardsCategoryId = new("11111111-1111-1111-1111-000000000005");
    public static readonly Guid OnlineCoursesCategoryId = new("11111111-1111-1111-1111-000000000006");

    public static readonly Guid NovaSoftBrandId = new("22222222-2222-2222-2222-000000000001");
    public static readonly Guid PixelForgeBrandId = new("22222222-2222-2222-2222-000000000002");
    public static readonly Guid InkwellPressBrandId = new("22222222-2222-2222-2222-000000000003");
    public static readonly Guid ChordCollectiveBrandId = new("22222222-2222-2222-2222-000000000004");

    private const string SeedCurrencyCode = "USD";
    private const string ProductIdPrefix = "33333333-3333-3333-3333-";

    private static readonly DateTimeOffset SeedTimestampUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<Category> Categories { get; } =
    [
        CreateCategory(SoftwareCategoryId, "Software", "Desktop applications, utilities and developer tools.", 1),
        CreateCategory(GamesCategoryId, "Games", "Downloadable games for PC and console.", 2),
        CreateCategory(EBooksCategoryId, "E-Books", "Digital books in EPUB and PDF formats.", 3),
        CreateCategory(MusicCategoryId, "Music", "Albums and singles in lossless and MP3 formats.", 4),
        CreateCategory(GiftCardsCategoryId, "Gift Cards", "Digital gift cards delivered by email.", 5),
        CreateCategory(OnlineCoursesCategoryId, "Online Courses", "Self-paced video courses with lifetime access.", 6),
    ];

    public static IReadOnlyList<Brand> Brands { get; } =
    [
        CreateBrand(NovaSoftBrandId, "NovaSoft"),
        CreateBrand(PixelForgeBrandId, "PixelForge Studios"),
        CreateBrand(InkwellPressBrandId, "Inkwell Press"),
        CreateBrand(ChordCollectiveBrandId, "Chord Collective"),
    ];

    public static IReadOnlyList<Product> Products { get; } =
    [
        CreateProduct(1, "TaskFlow Pro 2026", "Project planning suite with boards, timelines and workload insights.",
            "FC-SW-0001", 89.99m, SoftwareCategoryId, NovaSoftBrandId, 250,
            new ProductAttribute("Platform", "Windows, macOS"), new ProductAttribute("LicenseType", "Perpetual")),
        CreateProduct(2, "PhotoSmith Studio 8", "Non-destructive photo editor with RAW support and batch processing.",
            "FC-SW-0002", 129.99m, SoftwareCategoryId, NovaSoftBrandId, 180,
            new ProductAttribute("Platform", "Windows, macOS"), new ProductAttribute("LicenseType", "Perpetual")),
        CreateProduct(3, "SecureVault Password Manager", "Encrypted password vault with cross-device sync and breach alerts.",
            "FC-SW-0003", 34.99m, SoftwareCategoryId, NovaSoftBrandId, 420,
            new ProductAttribute("Platform", "Windows, macOS, Linux"), new ProductAttribute("LicenseType", "Annual")),
        CreateProduct(4, "CodeCanvas IDE Personal", "Polyglot IDE with refactoring, debugging and built-in profiler.",
            "FC-SW-0004", 159.00m, SoftwareCategoryId, NovaSoftBrandId, 140,
            new ProductAttribute("Platform", "Windows, macOS, Linux"), new ProductAttribute("LicenseType", "Annual")),

        CreateProduct(5, "Starlight Odyssey Deluxe", "Open-world space adventure, deluxe edition with the season pass.",
            "FC-GM-0001", 69.99m, GamesCategoryId, PixelForgeBrandId, 500,
            new ProductAttribute("Platform", "PC"), new ProductAttribute("AgeRating", "PEGI 12")),
        CreateProduct(6, "Harvest Lane Simulator", "Cosy farm-life simulator with seasons, markets and co-op play.",
            "FC-GM-0002", 29.99m, GamesCategoryId, PixelForgeBrandId, 460,
            new ProductAttribute("Platform", "PC"), new ProductAttribute("AgeRating", "PEGI 3")),
        CreateProduct(7, "Neon Drift Racing", "Arcade street racer with synthwave soundtrack and online leagues.",
            "FC-GM-0003", 39.99m, GamesCategoryId, PixelForgeBrandId, 380,
            new ProductAttribute("Platform", "PC"), new ProductAttribute("AgeRating", "PEGI 7")),
        CreateProduct(8, "Dungeon of Echoes", "Roguelike dungeon crawler with permadeath and daily challenge runs.",
            "FC-GM-0004", 49.99m, GamesCategoryId, PixelForgeBrandId, 320,
            new ProductAttribute("Platform", "PC"), new ProductAttribute("AgeRating", "PEGI 16")),

        CreateProduct(9, "The Pragmatic Kitchen Garden", "A practical guide to year-round vegetable growing in small spaces.",
            "FC-EB-0001", 12.99m, EBooksCategoryId, InkwellPressBrandId, 500,
            new ProductAttribute("Format", "EPUB, PDF"), new ProductAttribute("Pages", "248")),
        CreateProduct(10, "Distributed Systems in Practice", "Case-study driven tour of consensus, replication and failure modes.",
            "FC-EB-0002", 44.50m, EBooksCategoryId, InkwellPressBrandId, 500,
            new ProductAttribute("Format", "EPUB, PDF"), new ProductAttribute("Pages", "512")),
        CreateProduct(11, "A Field Guide to Night Skies", "Star charts and seasonal observation plans for naked-eye astronomy.",
            "FC-EB-0003", 18.75m, EBooksCategoryId, InkwellPressBrandId, 500,
            new ProductAttribute("Format", "EPUB, PDF"), new ProductAttribute("Pages", "304")),
        CreateProduct(12, "Sourdough Science", "The microbiology and technique behind reliable home sourdough.",
            "FC-EB-0004", 15.25m, EBooksCategoryId, InkwellPressBrandId, 500,
            new ProductAttribute("Format", "EPUB, PDF"), new ProductAttribute("Pages", "216")),

        CreateProduct(13, "Midnight Frequencies", "Downtempo electronica album, 12 tracks recorded live to tape.",
            "FC-MU-0001", 9.99m, MusicCategoryId, ChordCollectiveBrandId, 500,
            new ProductAttribute("Format", "FLAC, MP3"), new ProductAttribute("Tracks", "12")),
        CreateProduct(14, "Acoustic Mornings", "Stripped-back acoustic sessions for slow starts, 10 tracks.",
            "FC-MU-0002", 11.49m, MusicCategoryId, ChordCollectiveBrandId, 500,
            new ProductAttribute("Format", "FLAC, MP3"), new ProductAttribute("Tracks", "10")),
        CreateProduct(15, "Lo-Fi Study Sessions Vol. 3", "Two hours of instrumental lo-fi beats for deep focus.",
            "FC-MU-0003", 7.99m, MusicCategoryId, ChordCollectiveBrandId, 500,
            new ProductAttribute("Format", "FLAC, MP3"), new ProductAttribute("Tracks", "24")),
        CreateProduct(16, "Symphony No. 9 Remastered", "2026 remaster of the classic recording with restored dynamics.",
            "FC-MU-0004", 13.99m, MusicCategoryId, ChordCollectiveBrandId, 500,
            new ProductAttribute("Format", "FLAC, MP3"), new ProductAttribute("Tracks", "4")),

        CreateProduct(17, "NovaSoft Gift Card 25", "Redeemable against any NovaSoft application or upgrade.",
            "FC-GC-0001", 25.00m, GiftCardsCategoryId, NovaSoftBrandId, 500,
            new ProductAttribute("Delivery", "Email"), new ProductAttribute("Validity", "24 months")),
        CreateProduct(18, "PixelForge Game Credit 50", "Store credit for any PixelForge Studios title or season pass.",
            "FC-GC-0002", 50.00m, GiftCardsCategoryId, PixelForgeBrandId, 500,
            new ProductAttribute("Delivery", "Email"), new ProductAttribute("Validity", "24 months")),
        CreateProduct(19, "Inkwell Reading Voucher 20", "Gift any e-book in the Inkwell Press catalogue.",
            "FC-GC-0003", 20.00m, GiftCardsCategoryId, InkwellPressBrandId, 500,
            new ProductAttribute("Delivery", "Email"), new ProductAttribute("Validity", "24 months")),
        CreateProduct(20, "Chord Collective Music Pass 15", "Credit towards albums and singles from Chord Collective artists.",
            "FC-GC-0004", 15.00m, GiftCardsCategoryId, ChordCollectiveBrandId, 500,
            new ProductAttribute("Delivery", "Email"), new ProductAttribute("Validity", "24 months")),

        CreateProduct(21, "Mastering TypeScript", "From language fundamentals to production patterns in 14 hours of video.",
            "FC-OC-0001", 59.99m, OnlineCoursesCategoryId, NovaSoftBrandId, 300,
            new ProductAttribute("Duration", "14 hours"), new ProductAttribute("Level", "Intermediate")),
        CreateProduct(22, "Game Design Fundamentals", "Mechanics, balance and playtesting taught through four buildable projects.",
            "FC-OC-0002", 74.99m, OnlineCoursesCategoryId, PixelForgeBrandId, 220,
            new ProductAttribute("Duration", "18 hours"), new ProductAttribute("Level", "Beginner")),
        CreateProduct(23, "Creative Writing Workshop", "Eight guided modules on voice, structure and revision with exercises.",
            "FC-OC-0003", 39.99m, OnlineCoursesCategoryId, InkwellPressBrandId, 350,
            new ProductAttribute("Duration", "10 hours"), new ProductAttribute("Level", "Beginner")),
        CreateProduct(24, "Music Production Essentials", "Recording, mixing and mastering a track end to end in a home studio.",
            "FC-OC-0004", 64.99m, OnlineCoursesCategoryId, ChordCollectiveBrandId, 260,
            new ProductAttribute("Duration", "16 hours"), new ProductAttribute("Level", "Intermediate")),
    ];

    private static Category CreateCategory(Guid categoryId, string name, string description, int sortOrder) =>
        new()
        {
            Id = categoryId,
            Name = name,
            Slug = SlugGenerator.Generate(name),
            Description = description,
            ParentCategoryId = null,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedOnUtc = SeedTimestampUtc,
        };

    private static Brand CreateBrand(Guid brandId, string name)
    {
        var slug = SlugGenerator.Generate(name);

        return new Brand
        {
            Id = brandId,
            Name = name,
            Slug = slug,
            LogoUrl = $"https://picsum.photos/seed/{slug}/200/200",
            IsActive = true,
            CreatedOnUtc = SeedTimestampUtc,
        };
    }

    private static Product CreateProduct(
        int productNumber,
        string name,
        string description,
        string sku,
        decimal basePrice,
        Guid categoryId,
        Guid brandId,
        int initialStockQuantity,
        params ProductAttribute[] attributes) =>
        new()
        {
            Id = SeedProductId(productNumber),
            Name = name,
            Slug = SlugGenerator.Generate(name),
            Description = description,
            Sku = sku,
            BasePrice = basePrice,
            CurrencyCode = SeedCurrencyCode,
            CategoryId = categoryId,
            BrandId = brandId,
            IsActive = true,
            IsDigital = true,
            Images = [new ProductImage($"https://picsum.photos/seed/{sku}/640/480", name, IsPrimary: true)],
            Attributes = [.. attributes],
            InitialStockQuantity = initialStockQuantity,
            CreatedOnUtc = SeedTimestampUtc,
            UpdatedOnUtc = SeedTimestampUtc,
        };

    private static Guid SeedProductId(int productNumber) =>
        new(ProductIdPrefix + productNumber.ToString("D12", CultureInfo.InvariantCulture));
}
