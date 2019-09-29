#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: AuthenticationClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.Security;
	using System.Threading;

	using Ecng.Common;

	using StockSharp.Logging;

	/// <summary>
	/// The client for access to the StockSharp authentication service.
	/// </summary>
	public class AuthenticationClient : BaseServiceClient<IAuthenticationService>, IAuthenticationClient
	{
		private Timer _pingTimer;
		private readonly SyncObject _pingSync = new SyncObject();

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationClient"/>.
		/// </summary>
		public AuthenticationClient()
			: this(new Uri("https://stocksharp.com/services/authenticationservice.svc"))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationClient"/>.
		/// </summary>
		/// <param name="address">Service address.</param>
		public AuthenticationClient(Uri address)
			: base(address, "authentication")
		{
			Credentials = new ServerCredentials();
		}

		/// <inheritdoc />
		public ServerCredentials Credentials { get; }

		/// <inheritdoc />
		public Products? Product { get; set; }

		/// <inheritdoc />
		public Version Version { get; set; }

		/// <inheritdoc />
		public bool IsLoggedIn => NullableSessionId != null;

		/// <inheritdoc />
		public Guid SessionId
		{
			get
			{
				if (!IsLoggedIn)
					Login();

				return NullableSessionId.Value;
			}
		}

		/// <inheritdoc />
		public Guid? NullableSessionId { get; private set; }

		/// <inheritdoc />
		public long UserId { get; private set; }

		/// <inheritdoc />
		public void Login()
		{
			Login(Product, Version, Credentials.Email, Credentials.Password);
		}

		/// <inheritdoc />
		public void Login(Products? product, Version version, string login, SecureString password)
		{
			if (login.IsEmpty())
				throw new ArgumentNullException(nameof(login));

			if (password.IsEmpty())
				throw new ArgumentNullException(nameof(password));

			Guid sessionId;

			if (product == null)
			{
				sessionId = Invoke(f => f.Login(login, password.UnSecure()));
				sessionId.ToErrorCode().ThrowIfError();

				NullableSessionId = sessionId;
				UserId = Invoke(f => f.GetId(sessionId));
			}
			else
			{
				var tuple = Invoke(f => version == null
					? f.Login2(product.Value, login, password.UnSecure())
					: f.Login3(product.Value, version.To<string>(), login, password.UnSecure()));

				tuple.Item1.ToErrorCode().ThrowIfError();

				NullableSessionId = tuple.Item1;
				UserId = tuple.Item2;
			}

			if (product != null)
			{
				_pingTimer = ThreadingHelper.Timer(() =>
				{
					try
					{
						var session = NullableSessionId;

						if (session == null)
							return;

						lock (_pingSync)
						{
							Invoke(f => f.Ping(session.Value));
						}
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
				}).Interval(TimeSpan.FromMinutes(10));
			}
		}

		/// <inheritdoc />
		public void Logout()
		{
			Invoke(f => f.Logout(SessionId));
			NullableSessionId = null;
			UserId = 0;

			_pingTimer.Dispose();
		}

		/// <inheritdoc />
		public long GetId(Guid sessionId)
		{
			return Invoke(f => f.GetId(sessionId));
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			if (IsLoggedIn)
				Logout();

			base.DisposeManaged();
		}
	}
}