namespace StockSharp.ETrade
{
	using System;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.ETrade.Native;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to the ETrade.
	/// </summary>
	[Icon("ETrade_logo.png")]
	public class ETradeTrader : Connector
	{
		/// <summary>
		/// Key.
		/// </summary>
		public string ConsumerKey
		{
			get { return _adapter.ConsumerKey; }
			set { _adapter.ConsumerKey = value; }
		}

		/// <summary>
		/// Secret.
		/// </summary>
		public string ConsumerSecret
		{
			get { return _adapter.ConsumerSecret.To<string>(); }
			set { _adapter.ConsumerSecret = value.To<SecureString>(); }
		}

		/// <summary>
		/// OAuth access token. Required to restore connection. Saved AccessToken can be valid until EST midnight.
		/// </summary>
		public OAuthToken AccessToken
		{
			get { return _adapter.AccessToken; }
			set { _adapter.AccessToken = value; }
		}

		/// <summary>
		/// Verification code, received by user in browser, after confirming program's permission to work.
		/// </summary>
		public string VerificationCode
		{
			get { return _adapter.VerificationCode; }
			set { _adapter.VerificationCode = value; }
		}

		/// <summary>
		/// Sandbox mode.
		/// </summary>
		public bool Sandbox
		{
			get { return _adapter.Sandbox; }
			set { _adapter.Sandbox = value; }
		}

		private readonly ETradeMessageAdapter _adapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="ETradeTrader"/>.
		/// </summary>
		public ETradeTrader()
		{
			CreateAssociatedSecurity = true;

			_adapter = new ETradeMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter);

			ReConnectionSettings.TimeOutInterval = TimeSpan.FromMinutes(5);
			ReConnectionSettings.Interval = TimeSpan.FromMinutes(1);
			ReConnectionSettings.AttemptCount = 5;
		}

		/// <summary>
		/// Set own authorization mode (the default is browser uses).
		/// </summary>
		/// <param name="method">ETrade authorization method.</param>
		public void SetCustomAuthorizationMethod(Action<string> method)
		{
			_adapter.SetCustomAuthorizationMethod(method);
		}
	}
}