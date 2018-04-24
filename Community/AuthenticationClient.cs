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
	public class AuthenticationClient : BaseServiceClient<IAuthenticationService>
	{
		static AuthenticationClient()
		{
			_instance = new Lazy<AuthenticationClient>(() => new AuthenticationClient());
		}

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

		private static readonly Lazy<AuthenticationClient> _instance;

		/// <summary>
		/// The common authorization client for the whole application.
		/// </summary>
		public static AuthenticationClient Instance => _instance.Value;

		/// <summary>
		/// Information about the login and password for access to the StockSharp.
		/// </summary>
		public ServerCredentials Credentials { get; }

		/// <summary>
		/// Product.
		/// </summary>
		public Products? Product { get; set; }

		/// <summary>
		/// Product.
		/// </summary>
		public Version Version { get; set; }

		/// <summary>
		/// Has the client successfully authenticated.
		/// </summary>
		public bool IsLoggedIn => NullableSessionId != null;

		/// <summary>
		/// Session ID.
		/// </summary>
		public Guid SessionId
		{
			get
			{
				if (!IsLoggedIn)
					Login();

				return NullableSessionId.Value;
			}
		}

		/// <summary>
		/// To get the <see cref="SessionId"/> if the user was authorized.
		/// </summary>
		public Guid? NullableSessionId { get; private set; }

		/// <summary>
		/// The user identifier for <see cref="SessionId"/>.
		/// </summary>
		public long UserId { get; private set; }

		/// <summary>
		/// To log in.
		/// </summary>
		public void Login()
		{
			Login(Product, Credentials.Email, Credentials.Password);
		}

		/// <summary>
		/// To log in.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		public void Login(Products? product, string login, SecureString password)
		{
			if (login.IsEmpty())
				throw new ArgumentNullException(nameof(login));

			if (password.IsEmpty())
				throw new ArgumentNullException(nameof(password));

			Guid sessionId;

			if (product == null)
			{
				sessionId = Invoke(f => f.Login(login, password.To<string>()));
				sessionId.ToErrorCode().ThrowIfError();

				NullableSessionId = sessionId;
				UserId = Invoke(f => f.GetId(sessionId));
			}
			else
			{
				var tuple = Invoke(f => Version == null
					? f.Login2(product.Value, login, password.To<string>())
					: f.Login3(product.Value, Version.To<string>(), login, password.To<string>()));

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

		/// <summary>
		/// Logout.
		/// </summary>
		public void Logout()
		{
			Invoke(f => f.Logout(SessionId));
			NullableSessionId = null;
			UserId = 0;

			_pingTimer.Dispose();
		}

		/// <summary>
		/// Get a user id.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>User id.</returns>
		public long GetId(Guid sessionId)
		{
			return Invoke(f => f.GetId(sessionId));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (IsLoggedIn)
				Logout();

			base.DisposeManaged();
		}
	}
}