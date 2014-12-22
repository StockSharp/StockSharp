namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	public class AddStrategyInfoCommand : BaseStudioCommand
	{
		public StrategyInfo Info { get; private set; }

		public IEnumerable<StrategyInfoTypes> Types { get; set; }

		public AddStrategyInfoCommand(StrategyInfo info)
		{
			Info = info;
		}

		public AddStrategyInfoCommand(params StrategyInfoTypes[] types)
		{
			if (types == null)
				throw new ArgumentNullException("types");

			Types = types;
		}
	}
}