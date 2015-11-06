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
