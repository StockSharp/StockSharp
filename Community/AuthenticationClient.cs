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
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// The client for access to the StockSharp authentication service.
	/// </summary>
	public class AuthenticationClient : BaseServiceClient<IAuthenticationService>
	{
		static AuthenticationClient()
		{
			_instance = new Lazy<AuthenticationClient>(() => new AuthenticationClient());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationClient"/>.
		/// </summary>
		public AuthenticationClient()
			: this(new Uri("http://stocksharp.com/services/authenticationservice.svc"))
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
		/// Has the client successfully authenticated.
		/// </summary>
		public bool IsLoggedIn { get; private set; }

		private Guid _sessionId;

		/// <summary>
		/// Session ID.
		/// </summary>
		public Guid SessionId
		{
			get
			{
				if (!IsLoggedIn)
					Login();

				return _sessionId;
			}
		}

		/// <summary>
		/// To log in.
		/// </summary>
		public void Login()
		{
			Login(Credentials.Login, Credentials.Password.To<string>());
		}

		/// <summary>
		/// To log in.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		public void Login(string login, string password)
		{
			if (login.IsEmpty())
				throw new ArgumentNullException(nameof(login));

			if (password.IsEmpty())
				throw new ArgumentNullException(nameof(password));

			var sessionId = Invoke(f => f.Login(login, password));

			if (sessionId == Guid.Empty)
				throw new InvalidOperationException(LocalizedStrings.UnknownServerError);

			var bytes = sessionId.ToByteArray();
			if (bytes.Take(14).All(b => b == 0))
				((ErrorCodes)bytes[15]).ThrowIfError();

			_sessionId = sessionId;
			IsLoggedIn = true;
		}

		/// <summary>
		/// Logout.
		/// </summary>
		public void Logout()
		{
			Invoke(f => f.Logout(SessionId));
			IsLoggedIn = false;
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