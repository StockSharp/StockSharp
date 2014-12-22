namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class CloseWindowCommand : BaseStudioCommand
	{
		public string Id { get; private set; }

		public Type CtrlType { get; private set; }

		public CloseWindowCommand(string id, Type ctrlType)
		{
			if (ctrlType == null)
				throw new ArgumentNullException("ctrlType");

			Id = id;
			CtrlType = ctrlType;
		}
	}
}