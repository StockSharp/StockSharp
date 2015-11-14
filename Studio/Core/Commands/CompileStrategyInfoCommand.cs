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