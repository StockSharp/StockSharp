namespace StockSharp.Community
{
	using System;
	using System.Threading;

	using Ecng.Common;

	using StockSharp.Logging;

	/// <summary>
	/// The client for access to <see cref="ISessionService"/>.
	/// </summary>
	public class SessionClient : BaseCommunityClient<ISessionService>
	{
		private Timer _pingTimer;
		private readonly SyncObject _pingSync = new SyncObject();
		private long _sessionId;

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionClient"/>.
		/// </summary>
		public SessionClient()
			: this(new Uri("http://stocksharp.com/services/sessionservice.svc"))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionClient"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		public SessionClient(Uri address)
			: base(address, "session")
		{
		}

		/// <summary>
		/// TCreate a new activity session.
		/// </summary>
		/// <param name="product">Product type.</param>
		public void CreateSession(Products product)
		{
			if (_sessionId != 0)
				throw new InvalidOperationException();

			var authClient = AuthenticationClient.Instance;
			_sessionId = Invoke(f => f.CreateSession(product, authClient.IsLoggedIn ? authClient.SessionId : Guid.Empty));

			_pingTimer = ThreadingHelper.Timer(() =>
			{
				try
				{
					lock (_pingSync)
					{
						if (_sessionId == 0)
							return;

						Invoke(f => f.Ping(_sessionId));
					}
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}).Interval(TimeSpan.FromMinutes(10));
		}

		/// <summary>
		/// Close the session.
		/// </summary>
		public void CloseSession()
		{
			if (_sessionId == 0)
				throw new InvalidOperationException();

			_pingTimer.Dispose();

			lock (_pingSync)
			{
				Invoke(f => f.CloseSession(_sessionId));
				_sessionId = 0;
			}
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (_sessionId != 0)
				CloseSession();

			base.DisposeManaged();
		}
	}
}