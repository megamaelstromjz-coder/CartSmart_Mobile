using System.Collections.ObjectModel;
using CartSmart.Mobile.Models;

namespace CartSmart.Mobile.ViewModels;

/// <summary>A category bucket of list items for Shop mode's grouped <c>CollectionView</c>.</summary>
public class ItemGroup(string category, IEnumerable<ListItem> items) : ObservableCollection<ListItem>(items)
{
    public string Category { get; } = category;
}
