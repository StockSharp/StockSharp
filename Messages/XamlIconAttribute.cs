namespace StockSharp.Messages;

/// <summary>
/// Attribute for the icon.
/// </summary>
public class XamlIconAttribute : IconAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="XamlIconAttribute"/>.
	/// </summary>
	/// <param name="icon">Icon.</param>
	public XamlIconAttribute(string icon)
		: base($"/StockSharp.Xaml;component/IconsSvg/{icon}.svg", true)
	{
	}
}