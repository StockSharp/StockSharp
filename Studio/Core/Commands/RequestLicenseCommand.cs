namespace StockSharp.Studio.Core.Commands
{
	using System;

	using Ecng.Common;

	public class RequestLicenseCommand : BaseStudioCommand
	{
		public long BrokerId { get; private set; }

		public string Account { get; private set; }

		public RequestLicenseCommand(long brokerId, string account)
		{
			if (account.IsEmpty())
				throw new ArgumentNullException(nameof(account));

			BrokerId = brokerId;
			Account = account;
		}
	}
}