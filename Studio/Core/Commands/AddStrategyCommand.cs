#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: AddStrategyCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class AddStrategyCommand : BaseStudioCommand
	{
		public StrategyInfo Info { get; private set; }

		public StrategyContainer Strategy { get; set; }

		public SessionType SessionType { get; private set; }

		public AddStrategyCommand(StrategyInfo info, SessionType sessionType)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			Info = info;
			SessionType = sessionType;
		}

		public AddStrategyCommand(StrategyInfo info, StrategyContainer strategy, SessionType sessionType)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Info = info;
			Strategy = strategy;
			SessionType = sessionType;
		}
	}
}