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