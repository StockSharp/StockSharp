namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class EditSecurityCommand : BaseStudioCommand
	{
		public EditSecurityCommand(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			Security = security;
		}

		public Security Security { get; private set; }
	}
}