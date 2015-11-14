namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class OpenMarketDataSettingsCommand : BaseStudioCommand
	{
		public MarketDataSettings Settings { get; private set; }

		public OpenMarketDataSettingsCommand(MarketDataSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			Settings = settings;
		}
	}
}
