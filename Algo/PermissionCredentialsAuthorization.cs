namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Security;

	using StockSharp.Localization;

	/// <summary>
	/// The module of the connection access check based on the <see cref="PermissionCredentialsStorage"/> authorization.
	/// </summary>
	public class PermissionCredentialsAuthorization : IAuthorization
	{
		private readonly PermissionCredentialsStorage _storage;

		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionCredentialsAuthorization"/>.
		/// </summary>
		/// <param name="storage">Storage for <see cref="PermissionCredentials"/>.</param>
		public PermissionCredentialsAuthorization(PermissionCredentialsStorage storage)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
		}

		Guid IAuthorization.ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			if (login == null)
				throw new ArgumentNullException(nameof(login));

			if (password == null)
				throw new ArgumentNullException(nameof(password));

			var credentials = _storage.TryGetByLogin(login);

			if (credentials == null || !credentials.Password.IsEqualTo(password))
				throw new UnauthorizedAccessException(LocalizedStrings.WrongLoginOrPassword);

			if (credentials.IpRestrictions.Count > 0 && (clientAddress == null || !credentials.IpRestrictions.Contains(clientAddress)))
				throw new UnauthorizedAccessException(LocalizedStrings.IpAddrNotValid.Put(clientAddress));

			return Guid.NewGuid();
		}

		void IAuthorization.SaveUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses)
		{
			var user = _storage.TryGetByLogin(login);

			if (user == null)
			{
				user = new PermissionCredentials { Email = login };
				_storage.Add(user);
			}

			user.Password = password;

			lock (user.IpRestrictions.SyncRoot)
			{
				user.IpRestrictions.Clear();
				user.IpRestrictions.AddRange(possibleAddresses);
			}

			_storage.SaveCredentials();
		}

		bool IAuthorization.DeleteUser(string login)
		{
			if (!_storage.DeleteByLogin(login))
				return false;

			_storage.SaveCredentials();
			return true;
		}

		IEnumerable<Tuple<string, IEnumerable<IPAddress>>> IAuthorization.AllUsers =>
			_storage
				.Credentials
		        .Select(c => Tuple.Create(c.Email, (IEnumerable<IPAddress>)c.IpRestrictions.Cache))
		        .ToArray();
	}
}