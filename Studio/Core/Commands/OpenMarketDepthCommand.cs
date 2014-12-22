namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class OpenMarketDepthCommand : BaseStudioCommand
	{
		public Security Security { get; private set; }

		public OpenMarketDepthCommand(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			Security = security;
		}
	}
}
