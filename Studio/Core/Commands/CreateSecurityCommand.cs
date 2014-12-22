namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class CreateSecurityCommand : BaseStudioCommand
	{
		public override bool CanRouteToGlobalScope
		{
			get { return true; }
		}

		public CreateSecurityCommand(Type securityType)
		{
			if (securityType == null)
				throw new ArgumentNullException("securityType");

			SecurityType = securityType;
		}

		public Type SecurityType { get; private set; }

		public Security Security { get; set; }
	}
}