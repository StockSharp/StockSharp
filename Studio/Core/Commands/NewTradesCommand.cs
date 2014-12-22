namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	public class NewTradesCommand : BaseStudioCommand
	{
		public NewTradesCommand(IEnumerable<Trade> trades)
		{
			if (trades == null)
				throw new ArgumentNullException("trades");

			Trades = trades;
		}

		public IEnumerable<Trade> Trades { get; private set; }
	}
}