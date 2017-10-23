#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: BaseCommunityClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;

	using Ecng.Localization;

	using StockSharp.Localization;

	/// <summary>
	/// The base client for access to the StockSharp services.
	/// </summary>
	/// <typeparam name="TService">WCF service type.</typeparam>
	public abstract class BaseCommunityClient<TService> : BaseServiceClient<TService>
		where TService : class
	{
		/// <summary>
		/// Initialize <see cref="BaseCommunityClient{TService}"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="endpointName">The access point name in the configuration file.</param>
		/// <param name="hasCallbacks">Whether the <typeparamref name="TService" /> has events.</param>
		protected BaseCommunityClient(Uri address, string endpointName, bool hasCallbacks = false)
			: base(address, endpointName, hasCallbacks)
		{
		}

		/// <summary>
		/// The session identifier received from <see cref="IAuthenticationService.Login"/>.
		/// </summary>
		protected virtual Guid SessionId => AuthenticationClient.Instance.SessionId;

		/// <summary>
		/// To get the <see cref="SessionId"/> if the user was authorized.
		/// </summary>
		protected virtual Guid? NullableSessionId => AuthenticationClient.Instance.NullableSessionId;

		/// <summary>
		/// Is current language is English.
		/// </summary>
		protected static bool IsEnglish => LocalizedStrings.ActiveLanguage != Languages.Russian;
	}
}