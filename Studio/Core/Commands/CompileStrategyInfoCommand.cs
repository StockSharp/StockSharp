#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: CompileStrategyInfoCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Xaml.Actipro.Code;

	public class CompileStrategyInfoCommand : BaseStudioCommand
	{
		public CompileStrategyInfoCommand(StrategyInfo info, IEnumerable<CodeReference> references)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			if (references == null)
				throw new ArgumentNullException(nameof(references));

			Info = info;
			References = references;
		}

		public StrategyInfo Info { get; private set; }
		public IEnumerable<CodeReference> References { get; private set; }
	}

	public class CompileStrategyInfoResultCommand : BaseStudioCommand
	{
		public CompileStrategyInfoResultCommand(CompilationResult result)
		{
			if (result == null)
				throw new ArgumentNullException(nameof(result));

			Result = result;
		}

		public CompilationResult Result { get; private set; }
	}
}