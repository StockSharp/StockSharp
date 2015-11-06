namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class UpdateMarketDepthCommand : BaseStudioCommand
	{
		public UpdateMarketDepthCommand(MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			Depth = depth;
		}

		public MarketDepth Depth { get; private set; }
	}
}