#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: PnLChangedCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class PnLChangedCommand : BaseStudioCommand
	{
		public PnLChangedCommand(DateTimeOffset time, decimal totalPnL, decimal unrealizedPnL, decimal? commission)
		{
			Time = time;
			TotalPnL = totalPnL;
			UnrealizedPnL = unrealizedPnL;
			Commission = commission;
		}

		public decimal TotalPnL { get; private set; }
		public decimal UnrealizedPnL { get; private set; }
		public decimal? Commission { get; set; }
		public DateTimeOffset Time { get; private set; }
	}
}