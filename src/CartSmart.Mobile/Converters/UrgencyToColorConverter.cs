using System.Globalization;
using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.Converters;

public class UrgencyToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var resourceKey = value is SuggestionUrgency urgency
            ? urgency switch
            {
                SuggestionUrgency.Overdue => "StatusOverdue",
                SuggestionUrgency.DueSoon => "StatusDueSoon",
                _ => "StatusUpcoming",
            }
            : "StatusUpcoming";

        return Application.Current?.Resources.TryGetValue(resourceKey, out var color) == true
            ? color
            : Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
