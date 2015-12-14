#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: RemoveLicenseCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
				throw new ArgumentNullException(nameof(license));

			License = license;
		}
	}
}