#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: RequestLicenseCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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