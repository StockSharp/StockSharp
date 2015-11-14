namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	public class NewMyTradesCommand : BaseStudioCommand
	{
		public NewMyTradesCommand(IEnumerable<MyTrade> trades)
		{
			if (trades == null)
				throw new ArgumentNullException(nameof(trades));

			Trades = trades;
		}

		public IEnumerable<MyTrade> Trades { get; private set; }
	}
}