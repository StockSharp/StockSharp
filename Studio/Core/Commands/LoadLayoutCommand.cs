namespace StockSharp.Studio.Core.Commands
{
	using System;

	using Ecng.Common;

	public class LoadLayoutCommand : BaseStudioCommand
	{
		public LoadLayoutCommand(string layout)
		{
			if (layout.IsEmpty())
				throw new ArgumentNullException("layout");

			Layout = layout;
		}

		public string Layout { get; private set; }
	}

	public class SaveLayoutCommand : BaseStudioCommand
	{
		public string Layout { get; set; }
	}
}