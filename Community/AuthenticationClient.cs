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
	using Ecng.ComponentModel;

	using StockSharp.Community.Messages;
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

		/// <summary>
		/// Authorize by token.
		/// </summary>
		public bool IsToken { get; set; }

		/// <summary>
		/// Disable refresh.
		/// </summary>
		public bool DisableRefresh { get; set; }

		/// <inheritdoc />
		public ProductInfoMessage Product { get; set; }

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
		public Tuple<Guid, long> Login()
		{
			return Login(Product, Version, Credentials.Email, Credentials.Password);
		}

		/// <inheritdoc />
		public Tuple<Guid, long> Login(ProductInfoMessage product, Version version, SecureString token)
		{
			return HandleResponse(product, Invoke(f => f.Login5(product?.Id ?? 0, version.To<string>(), token.UnSecure())));
		}

		/// <inheritdoc />
		public Tuple<Guid, long> Login(ProductInfoMessage product, Version version, string login, SecureString password)
		{
			//if (login.IsEmpty())
			//	throw new ArgumentNullException(nameof(login));

			if (password.IsEmpty())
				throw new ArgumentNullException(nameof(password));

			return HandleResponse(product, Invoke(f =>
				IsToken
					? f.Login5(product?.Id ?? 0, version.To<string>(), password.UnSecure())
					: f.Login4(product?.Id ?? 0, version.To<string>(), login, password.UnSecure())));
		}

		private Tuple<Guid, long> HandleResponse(ProductInfoMessage product, Tuple<Guid, long> tuple)
		{
			if (tuple is null)
				throw new ArgumentNullException(nameof(tuple));

			tuple.Item1.ToErrorCode().ThrowIfError();

			if (product != null && !DisableRefresh)
			{
				NullableSessionId = tuple.Item1;
				UserId = tuple.Item2;

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

			return tuple;
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