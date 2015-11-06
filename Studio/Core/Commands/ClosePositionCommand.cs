namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class ClosePositionCommand : BaseStudioCommand
	{
		public Position Position { get; private set; }

		public ClosePositionCommand()
		{
		}

		public ClosePositionCommand(Position position)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			Position = position;
		}
	}
}