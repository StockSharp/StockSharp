namespace StockSharp.Localization
{
	using Ecng.ComponentModel;

	/// <summary>
	/// Specify icon, located in S#.Media library.
	/// </summary>
	public class StockSharpIconAttribute : IconAttribute
	{
		/// <summary>
		/// Create <see cref="StockSharpIconAttribute"/>.
		/// </summary>
		/// <param name="icon">Icon url.</param>
		public StockSharpIconAttribute(string icon)
			: base($"/StockSharp.Media;component/logos/{icon}", true)
		{
		}
	}
}