namespace SampleDiagram
{
	using System.Windows.Input;

	using Ecng.Localization;
	using Ecng.Xaml;

	using StockSharp.Localization;

	public partial class App
	{
		public App()
		{
			LocalizedStrings.ActiveLanguage = Languages.English;
			GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(CommandManager.InvalidateRequerySuggested);
		}
	}
}