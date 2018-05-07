namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Security;

	using StockSharp.Localization;

	/// <summary>
	/// The connection access check module based on the <see cref="PermissionCredentialsStorage"/> authentication.
	/// </summary>
	public class PermissionCredentialsRemoteAuthorization : AnonymousRemoteAuthorization
	{
		private readonly SynchronizedDictionary<Guid, PermissionCredentials> _sessions = new SynchronizedDictionary<Guid, PermissionCredentials>();
		private readonly PermissionCredentialsStorage _storage;

		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionCredentialsRemoteAuthorization"/>.
		/// </summary>
		/// <param name="storage">Storage for <see cref="PermissionCredentials"/>.</param>
		public PermissionCredentialsRemoteAuthorization(PermissionCredentialsStorage storage)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
		}

		private IAuthorization _authorization = new AnonymousAuthorization();

		/// <summary>
		/// Authorization module.
		/// </summary>
		public IAuthorization Authorization
		{
			get => _authorization;
			set => _authorization = value ?? throw new ArgumentNullException(nameof(value));
		}

		private bool IsAnonymous => Authorization is AnonymousAuthorization;

		/// <inheritdoc />
		public override IEnumerable<Tuple<string, IEnumerable<IPAddress>, UserPermissions>> AllRemoteUsers
		{
			get
			{
				if (IsAnonymous)
					return base.AllRemoteUsers;
				else
				{
					return _storage.Credentials.Select(c =>
					{
						IEnumerable<IPAddress> addresses;

						lock (c.IpRestrictions.SyncRoot)
							addresses = c.IpRestrictions.ToArray();

						UserPermissions permissions;

						lock (c.Permissions.SyncRoot)
							permissions = c.Permissions.Keys.JoinMask();

						return Tuple.Create(c.Email, addresses, permissions);
					}).ToArray();
				}
			}
		}

		/// <inheritdoc />
		public override void SaveRemoteUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses, UserPermissions permissions)
		{
			if (IsAnonymous)
				return;

			possibleAddresses = possibleAddresses.ToArray();

			_authorization.SaveUser(login, password, possibleAddresses);

			var user = _storage.TryGetByLogin(login);

			if (user == null)
			{
				user = new PermissionCredentials
				{
					Email = login
				};

				_storage.Add(user);
			}

			lock (user.IpRestrictions.SyncRoot)
			{
				user.IpRestrictions.Clear();
				user.IpRestrictions.AddRange(possibleAddresses);
			}

			lock (user.Permissions.SyncRoot)
			{
				user.Permissions.Clear();

				foreach (var part in permissions.SplitMask())
				{
					user.Permissions.SafeAdd(part);
				}
			}

			_storage.SaveCredentials();
		}

		/// <inheritdoc />
		public override bool DeleteRemoteUser(string login)
		{
			if (IsAnonymous)
				return false;

			if (!_authorization.DeleteUser(login))
				return false;

			_storage.DeleteByLogin(login);
			_storage.SaveCredentials();
			return true;
		}

		/// <inheritdoc />
		public override Guid ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			var sessionId = _authorization.ValidateCredentials(login, password, clientAddress);

			if (IsAnonymous)
				return sessionId;

			var credentials = _storage.TryGetByLogin(login);

			if (credentials == null)
				throw new UnauthorizedAccessException(LocalizedStrings.UserNotFound.Put(login));

			_sessions.Add(sessionId, credentials);

			return sessionId;
		}

		/// <inheritdoc />
		public override bool HasPermissions(Guid sessionId, UserPermissions requiredPermissions, string securityId, string dataType, object arg, DateTime? date)
		{
			if (IsAnonymous)
				return base.HasPermissions(sessionId, requiredPermissions, securityId, dataType, arg, date);

			var credentials = _sessions.TryGetValue(sessionId);

			var dict = credentials?.Permissions.TryGetValue(requiredPermissions);

			if (dict == null)
				return false;

			if (dict.Count == 0)
				return true;

			return dict.TryGetValue(Tuple.Create(securityId, dataType, arg, date));
		}
	}
}