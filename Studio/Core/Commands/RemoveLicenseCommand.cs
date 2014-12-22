namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.Licensing;

	public class RemoveLicenseCommand : BaseStudioCommand
	{
		public License License { get; private set; }

		public RemoveLicenseCommand(License license)
		{
			if (license == null)
				throw new ArgumentNullException("license");

			License = license;
		}
	}
}