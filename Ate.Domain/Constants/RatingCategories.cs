namespace Ate.Domain.Constants;

public static class RatingCategories
{
    public const string Punctual = "punctual";
    public const string Accurate = "accurate";
    public const string Tasty = "tasty";
    public const string Packaging = "packaging";

    public static readonly IReadOnlyList<string> All = [Punctual, Accurate, Tasty, Packaging];

    public static bool AppliesTo(string category, string deliveryMode) =>
        category != Packaging || deliveryMode is "retiro" or "entrega";
}
