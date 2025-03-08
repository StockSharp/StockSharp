namespace StockSharp.Messages;

/// <summary>
/// Attribute for the icon.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="XamlIconAttribute"/>.
/// </remarks>
/// <param name="icon">Icon.</param>
public class XamlIconAttribute(string icon) : IconAttribute($"/StockSharp.Xaml;component/IconsSvg/{icon}.svg", true)
{
}