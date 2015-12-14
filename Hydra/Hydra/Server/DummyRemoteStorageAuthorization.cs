#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Server.HydraPublic
File: DummyRemoteStorageAuthorization.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Server
{
	using System;

	using Ecng.Security;

	using StockSharp.Algo.History.Hydra;
	using StockSharp.Localization;

	class DummyRemoteStorageAuthorization : IRemoteStorageAuthorization
	{
		Guid IAuthorization.ValidateCredentials(string login, string password)
		{
			throw new UnauthorizedAccessException(LocalizedStrings.Str2873);
		}

		RemoteStoragePermissions IRemoteStorageAuthorization.GetPermissions(Guid sessionId, string securityId, string dataType, object arg, DateTime date)
		{
			return 0;
		}
	}
}