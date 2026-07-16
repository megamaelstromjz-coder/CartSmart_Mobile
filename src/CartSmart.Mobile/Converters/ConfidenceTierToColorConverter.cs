using System.Globalization;

namespace CartSmart.Mobile.Converters;

public class ConfidenceTierToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var resourceKey = value as string switch
        {
            "High confidence" => "ConfidenceHigh",
            "Building data" => "StatusDueSoon",
            _ => "TextMuted",
        };

        return Application.Current?.Resources.TryGetValue(resourceKey, out var color) == true
            ? color
            : Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
