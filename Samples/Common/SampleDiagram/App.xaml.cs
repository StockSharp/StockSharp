namespace SampleDiagram
{
	using System.Windows.Input;

	using Ecng.Xaml;

	public partial class App
	{
		public App()
		{
			GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(CommandManager.InvalidateRequerySuggested);
		}
	}
}