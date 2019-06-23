#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: CommunityAuthorization.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Security;

	using Ecng.Security;

	using StockSharp.Localization;

	/// <summary>
	/// The module of the connection access check based on the <see cref="IAuthenticationService"/> authorization.
	/// </summary>
	public class CommunityAuthorization : IAuthorization
	{
		private readonly Products _product;
		private readonly Version _version;
		private readonly IAuthenticationClient _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommunityAuthorization"/>.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="version">Version.</param>
		/// <param name="client">The client for access to the StockSharp authentication service.</param>
		public CommunityAuthorization(Products product, Version version, IAuthenticationClient client)
		{
			_product = product;
			_version = version ?? throw new ArgumentNullException(nameof(version));
			_client = client ?? throw new ArgumentNullException(nameof(client));
		}

		/// <summary>
		/// To check the username and password on correctness.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <param name="clientAddress">Remote network address.</param>
		/// <returns>Session ID.</returns>
		public virtual Guid ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			try
			{
				_client.Login(_product, _version, login, password);
				return Guid.NewGuid();
			}
			catch (Exception ex)
			{
				throw new UnauthorizedAccessException(LocalizedStrings.WrongLoginOrPassword, ex);
			}
		}

		/// <summary>
		/// Save user.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <param name="possibleAddresses">Possible addresses.</param>
		public void SaveUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Change password.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="oldPassword">Old password.</param>
		/// <param name="newPassword">New password.</param>
		public void ChangePassword(string login, SecureString oldPassword, SecureString newPassword)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Delete user by login.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <returns>Returns <see langword="true"/>, if user was deleted, otherwise return <see langword="false"/>.</returns>
		public bool DeleteUser(string login)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Get all available users.
		/// </summary>
		public IEnumerable<Tuple<string, IEnumerable<IPAddress>>> AllUsers => throw new NotSupportedException();
	}
}