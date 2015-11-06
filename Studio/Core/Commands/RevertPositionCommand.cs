namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class RevertPositionCommand : BaseStudioCommand
	{
		public Position Position { get; private set; }

		public RevertPositionCommand()
		{
		}

		public RevertPositionCommand(Position position)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			Position = position;
		}
	}
}