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
	using StockSharp.Messages;

	/// <summary>
	/// The connection access check module based on the <see cref="PermissionCredentialsStorage"/> authentication.
	/// </summary>
	public class PermissionCredentialsRemoteAuthorization : AnonymousRemoteAuthorization
	{
		private readonly SynchronizedDictionary<Guid, PermissionCredentials> _sessions = new SynchronizedDictionary<Guid, PermissionCredentials>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionCredentialsRemoteAuthorization"/>.
		/// </summary>
		/// <param name="storage">Storage for <see cref="PermissionCredentials"/>.</param>
		public PermissionCredentialsRemoteAuthorization(PermissionCredentialsStorage storage)
		{
			Storage = storage ?? throw new ArgumentNullException(nameof(storage));
		}

		/// <summary>
		/// Storage for <see cref="PermissionCredentials"/>.
		/// </summary>
		public PermissionCredentialsStorage Storage { get; }

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
		public override IEnumerable<PermissionCredentials> AllRemoteUsers
			=> IsAnonymous ? base.AllRemoteUsers : Storage.Credentials;

		/// <inheritdoc />
		public override void SaveRemoteUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses, UserPermissions permissions)
		{
			if (IsAnonymous)
				return;

			possibleAddresses = possibleAddresses.ToArray();

			_authorization.SaveUser(login, password, possibleAddresses);

			var user = Storage.TryGetByLogin(login);

			if (user == null)
			{
				user = new PermissionCredentials
				{
					Email = login
				};

				Storage.Add(user);
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

			Storage.SaveCredentials();
		}

		/// <inheritdoc />
		public override bool DeleteRemoteUser(string login)
		{
			if (IsAnonymous)
				return false;

			if (!_authorization.DeleteUser(login))
				return false;

			Storage.DeleteByLogin(login);
			Storage.SaveCredentials();
			return true;
		}

		/// <summary>
		/// Validate credentials.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <param name="clientAddress">IP address.</param>
		/// <returns>Session ID.</returns>
		public override Guid ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			var sessionId = _authorization.ValidateCredentials(login, password, clientAddress);

			if (IsAnonymous)
				return sessionId;

			_sessions.Add(sessionId, GetCredentials(login));

			return sessionId;
		}

		/// <summary>
		/// Get credentials by specified login.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <returns>Credentials with set of permissions.</returns>
		protected virtual PermissionCredentials GetCredentials(string login)
		{
			var credentials = Storage.TryGetByLogin(login);

			if (credentials == null)
				throw new UnauthorizedAccessException(LocalizedStrings.UserNotFound.Put(login));

			return credentials;
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