namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class PositionEditCommand : BaseStudioCommand
	{
		public BasePosition Position { get; private set; }

		public PositionEditCommand(BasePosition position)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			Position = position;
		}
	}
}