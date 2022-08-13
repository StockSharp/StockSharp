namespace StockSharp.Configuration
{
	using Ecng.ComponentModel;

	/// <summary>
	/// Specify icon, located in S#.Xaml library.
	/// </summary>
	public class SvgIconAttribute : IconAttribute
	{
		/// <summary>
		/// Create <see cref="SvgIconAttribute"/>.
		/// </summary>
		/// <param name="icon">Icon url.</param>
		public SvgIconAttribute(string icon)
			: base($"/StockSharp.Xaml;component/IconsSvg/{icon}", true)
		{
		}
	}
}
