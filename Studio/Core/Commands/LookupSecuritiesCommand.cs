namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class LookupSecuritiesCommand : BaseStudioCommand
	{
		public LookupSecuritiesCommand(Security criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			Criteria = criteria;
		}

		public Security Criteria { get; private set; }
	}
}