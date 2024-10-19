namespace StockSharp.Localization;

using Ecng.ComponentModel;

/// <summary>
/// Specify icon, located in S#.Media library.
/// </summary>
public class MediaIconAttribute : IconAttribute
{
	/// <summary>
	/// Create <see cref="MediaIconAttribute"/>.
	/// </summary>
	/// <param name="icon">Icon url.</param>
	public MediaIconAttribute(string icon)
		: base($"/StockSharp.Media;component/logos/{icon}", true)
	{
	}
}