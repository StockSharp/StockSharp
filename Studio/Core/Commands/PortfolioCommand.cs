namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class PortfolioCommand : BaseStudioCommand
	{
		public PortfolioCommand(Portfolio portfolio, bool isNew)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			Portfolio = portfolio;
			IsNew = isNew;
		}

		public Portfolio Portfolio { get; private set; }
		public bool IsNew { get; private set; }
	}
}