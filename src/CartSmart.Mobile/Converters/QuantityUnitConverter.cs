using System.Globalization;

namespace CartSmart.Mobile.Converters;

/// <summary>Formats a ListItem's Quantity+Unit into "2 L" / "1 unit" style display text.</summary>
public class QuantityUnitConverter : IMultiValueConverter
{
    public object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [double quantity, ..])
        {
            return string.Empty;
        }

        var unit = values.Length > 1 && values[1] is string { Length: > 0 } u ? u : "unit";
        var quantityText = quantity % 1 == 0
            ? quantity.ToString("0", culture)
            : quantity.ToString("0.##", culture);

        return $"{quantityText} {unit}";
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
