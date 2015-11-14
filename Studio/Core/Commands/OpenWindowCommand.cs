namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class OpenWindowCommand : BaseStudioCommand
	{
		public OpenWindowCommand(string id, Type ctrlType, bool isToolWindow, object context = null)
		{
			if (ctrlType == null)
				throw new ArgumentNullException(nameof(ctrlType));

			Id = id;
			CtrlType = ctrlType;
			IsToolWindow = isToolWindow;
			Context = context;
		}

		public string Id { get; private set; }
		public Type CtrlType { get; private set; }
		public bool IsToolWindow { get; private set; }
		public object Context { get; private set; }
	}
}