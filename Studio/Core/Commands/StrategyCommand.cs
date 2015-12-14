#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: StrategyCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class StartStrategyCommand : BaseStudioCommand
	{
		public StrategyContainer Strategy { get; private set; }

		public DateTime? StartDate { get; private set; }

		public DateTime? StopDate { get; private set; }

		public TimeSpan? CandlesTimeFrame { get; private set; }

		public bool OnlyInitialize { get; private set; }

		public bool Step { get; private set; }

        public StartStrategyCommand(StrategyContainer strategy, bool step = false)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Strategy = strategy;
			Step = step;
		}

		public StartStrategyCommand(StrategyContainer strategy, DateTime startDate, DateTime stopDate, TimeSpan? candlesTimeFrame, bool onlyInitialize)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Strategy = strategy;
			StartDate = startDate;
			StopDate = stopDate;
			CandlesTimeFrame = candlesTimeFrame;
			OnlyInitialize = onlyInitialize;
		}
	}

	public class StopStrategyCommand : BaseStudioCommand
	{
        public StrategyContainer Strategy { get; private set; }

        public StopStrategyCommand(StrategyContainer strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Strategy = strategy;
		}
	}
}
