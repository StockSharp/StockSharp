#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: CommunityAuthorization.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;

	using Ecng.Security;

	using StockSharp.Localization;

	/// <summary>
	/// The module of the connection access check based on the <see cref="IAuthenticationService"/> authorization.
	/// </summary>
	public class CommunityAuthorization : IAuthorization
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommunityAuthorization"/>.
		/// </summary>
		public CommunityAuthorization()
		{
		}

		/// <summary>
		/// To check the username and password on correctness.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <returns>Session ID.</returns>
		public virtual Guid ValidateCredentials(string login, string password)
		{
			try
			{
				AuthenticationClient.Instance.Login(login, password);
				return Guid.NewGuid();
			}
			catch (Exception ex)
			{
				throw new UnauthorizedAccessException(LocalizedStrings.WrongLoginOrPassword, ex);
			}
		}
	}
}