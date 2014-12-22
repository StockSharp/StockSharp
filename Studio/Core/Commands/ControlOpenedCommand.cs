namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class ControlOpenedCommand : BaseStudioCommand
	{
		public IStudioControl Control { get; private set; }

		public bool IsMainWindow { get; set; }

		public ControlOpenedCommand(IStudioControl control, bool isMainWindow)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			Control = control;
			IsMainWindow = isMainWindow;
		}
	}
}
