namespace StockSharp.Xaml.Actipro
{
	using System.Windows;

	using StockSharp.Xaml.Charting;

	/// <summary>
	/// The class for Actipro based applications.
	/// </summary>
	public abstract class ActiproBaseApplication : ExtendedBaseApplication
	{
		/// <summary>
		/// Processing the application start.
		/// </summary>
		/// <param name="e">Argument.</param>
		protected override void OnStartup(StartupEventArgs e)
		{
			Extensions.TranslateActiproDocking();
			Extensions.TranslateActiproNavigation();

			base.OnStartup(e);
		}
	}
}