namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.Licensing;

	public class RenewLicenseCommand : BaseStudioCommand
	{
		public License License { get; private set; }

		public RenewLicenseCommand()
		{
		}

		public RenewLicenseCommand(License license)
		{
			if (license == null)
				throw new ArgumentNullException("license");

			License = license;
		}
	}
}