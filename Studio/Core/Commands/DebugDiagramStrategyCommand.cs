#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: DebugDiagramStrategyCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

    public class DebugDiagramStrategyCommand : BaseStudioCommand
	{
		public StrategyContainer Strategy { get; private set; }

        public DebugDiagramStrategyCommand(StrategyContainer strategy)
		{
			if (strategy == null) 
				throw new ArgumentNullException(nameof(strategy));

			Strategy = strategy;
		}
	}
}
