using System.Globalization;

namespace CartSmart.Mobile.Converters;

public class CategoryToColorConverter : IValueConverter
{
    private static readonly Dictionary<string, string> CategoryResourceKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dairy"] = "CategoryDairy",
        ["Bakery"] = "CategoryBakery",
        ["Produce"] = "CategoryProduce",
        ["Meat & Seafood"] = "CategoryMeatSeafood",
        ["Beverages"] = "CategoryBeverages",
        ["Household"] = "CategoryHousehold",
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var resourceKey = value is string category && CategoryResourceKeys.TryGetValue(category, out var key)
            ? key
            : "CategoryDefault";

        return Application.Current?.Resources.TryGetValue(resourceKey, out var color) == true
            ? color
            : Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
