namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class ControlChangedCommand : BaseStudioCommand
	{
		public ControlChangedCommand(IStudioControl control)
		{
			if (control == null)
				throw new ArgumentNullException("control");

			Control = control;
		}

		public IStudioControl Control { get; private set; }

		public override bool CanRouteToGlobalScope
		{
			get { return true; }
		}
	}
}