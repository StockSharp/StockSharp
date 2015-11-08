namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	public class PositionCommand : BaseStudioCommand
	{
		public PositionCommand(DateTimeOffset time, KeyValuePair<Tuple<SecurityId, string>, decimal> position, bool isNew)
		{
			//if (position == null)
			//	throw new ArgumentNullException("position");

			Time = time;
			Position = position;
			IsNew = isNew;
		}

		public DateTimeOffset Time { get; private set; }
		public KeyValuePair<Tuple<SecurityId, string>, decimal> Position { get; private set; }
		public bool IsNew { get; private set; }
	}
}