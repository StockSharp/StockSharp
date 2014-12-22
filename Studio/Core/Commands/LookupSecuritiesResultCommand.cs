namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	public class LookupSecuritiesResultCommand : BaseStudioCommand
	{
		public LookupSecuritiesResultCommand(IEnumerable<Security> securities)
		{
			if (securities == null)
				throw new ArgumentNullException("securities");

			Securities = securities;
		}

		public IEnumerable<Security> Securities { get; private set; }
	}
}