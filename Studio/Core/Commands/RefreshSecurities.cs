#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: RefreshSecurities.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	public class RefreshSecurities : BaseStudioCommand
	{
		public MarketDataSettings Settings { get; private set; }

		public IEnumerable<SecurityTypes> Types { get; private set; }

		public Func<bool> IsCancelled { get; private set; }

		public Action<int> ProgressChanged { get; private set; }

		public Action<int> WhenFinished { get; private set; }

		public RefreshSecurities(MarketDataSettings settings, IEnumerable<SecurityTypes> types, Func<bool> isCancelled, Action<int> progressChanged, Action<int> whenFinished)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			if (types == null)
				throw new ArgumentNullException(nameof(types));

			if (isCancelled == null)
				throw new ArgumentNullException(nameof(isCancelled));

			if (progressChanged == null)
				throw new ArgumentNullException(nameof(progressChanged));

			if (whenFinished == null)
				throw new ArgumentNullException(nameof(whenFinished));

			Settings = settings;
			Types = types;
			IsCancelled = isCancelled;
			ProgressChanged = progressChanged;
			WhenFinished = whenFinished;
		}
	}
}
