using System.Globalization;

namespace Restaurant_Aqa_Framework.Support;

/// <summary>One line of a guest's cart: a dish (by the short label of the feature file) and how many.</summary>
public sealed record CartLine(string Dish, int Quantity);

/// <summary>One dish the restaurant offers in a scenario: its label and price.</summary>
public sealed record DishOffer(string Dish, decimal Price);

/// <summary>Reads the data tables and short strings feature files use to describe dishes and money.</summary>
public static class TableReaders
{
    public static IReadOnlyList<CartLine> CartFrom(DataTable table) =>
        table.Rows.Select(row => new CartLine(row["dish"], int.Parse(row["quantity"], CultureInfo.InvariantCulture))).ToList();

    public static IReadOnlyList<DishOffer> OffersFrom(DataTable table) =>
        table.Rows.Select(row => new DishOffer(row["dish"], Money(row["price"]))).ToList();

    /// <summary>"3 soup, 1 steak" -> soup 3, steak 1.</summary>
    public static IReadOnlyList<CartLine> DishCounts(string text) =>
        text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part =>
            {
                var cut = part.IndexOf(' ', StringComparison.Ordinal);
                return new CartLine(part[(cut + 1)..].Trim(), int.Parse(part[..cut], CultureInfo.InvariantCulture));
            })
            .ToList();

    /// <summary>"17.00", "$17.00" or "$1,017.00" -> 17.00, 17.00, 1017.00.</summary>
    public static decimal Money(string text) =>
        decimal.Parse(text.Replace("$", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal).Trim(), NumberStyles.Number, CultureInfo.InvariantCulture);

    /// <summary>A comparable text for a set of dishes: sorted by label, "soup × 3, steak × 1".</summary>
    public static string Describe(IEnumerable<CartLine> lines) =>
        string.Join(", ", lines.GroupBy(line => line.Dish, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key} × {group.Sum(line => line.Quantity)}"));
}
