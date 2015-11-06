namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class NewNewsCommand : BaseStudioCommand
	{
		public NewNewsCommand(News news)
		{
			if (news == null)
				throw new ArgumentNullException(nameof(news));

			News = news;
		}

		public News News { get; private set; }
	}
}