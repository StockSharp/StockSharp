namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class PositionCommand : BaseStudioCommand
	{
		public PositionCommand(DateTimeOffset time, Position position, bool isNew)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			Time = time;
			Position = position;
			IsNew = isNew;
		}

		public DateTimeOffset Time { get; private set; }
		public Position Position { get; private set; }
		public bool IsNew { get; private set; }
	}
}