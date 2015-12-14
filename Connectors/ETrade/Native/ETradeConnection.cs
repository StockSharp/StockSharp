#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.Native.ETrade
File: ETradeConnection.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade.Native
{
	using System;

	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.ETrade.Xaml;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	internal class ETradeConnection
	{
		private Action<string> _authorizationAction;
		
		public Action<string> AuthorizationAction
		{
			get { return _authorizationAction; }
			set { _authorizationAction = value ?? DefaultAuthorizationAction; }
		}

		public bool IsConnected { get; private set; }
		public OAuthToken AccessToken { get; internal set; }

		private readonly ETradeClient _client;

		bool _reconnecting;

		private ETradeApi Api
		{
			get { return _client.Api; }
		}

		private OAuthToken _reqToken;

		public ETradeConnection(ETradeClient client)
		{
			_client = client;
			_authorizationAction = DefaultAuthorizationAction;
		}

		public event Action ConnectionStateChanged;

		public void Connect()
		{
			if (IsConnected)
			{
				_client.AddWarningLog("Connect: already connected");
				return;
			}

			do
			{
				if (!_client.VerificationCode.IsEmpty() && _reqToken != null)
				{
					try
					{
						_client.AddDebugLog("getting access token with code {0}", _client.VerificationCode);
						AccessToken = Api.GetAccessToken(_reqToken, _client.VerificationCode);
						SetConnectionState(true);
						return;
					}
					catch (ETradeException)
					{
						_client.VerificationCode = null;
						throw;
					}
				}

				if (AccessToken != null)
				{
					try
					{
						AccessToken = Api.RenewAccessToken(AccessToken);
						_client.AddDebugLog("renew of access token successful");
						SetConnectionState(true);
						return;
					}
					catch (Exception e)
					{
						_client.AddWarningLog("unable to renew access token: {0}", e);
						AccessToken = null;
					}
				}

				_client.AddDebugLog("getting request token");
				_reqToken = Api.GetRequestToken();

				_client.VerificationCode = null;

				_client.AddDebugLog("starting authorization");
				AuthorizationAction(Api.GetAuthorizeUrl(_reqToken));
			}
			while (!_client.VerificationCode.IsEmpty());
		}

		private void DefaultAuthorizationAction(string authUrl)
		{
			var res = GuiObjectHelper.ShowDialog(() => new AuthDialog(authUrl), wnd => _client.VerificationCode = wnd.VerificationCode);

			if (!res)
				throw new ETradeAuthorizationFailedException(LocalizedStrings.Str3358);
		}

		public void Disconnect()
		{
			SetConnectionState(false);
		}

		public void ReconnectAsync()
		{
			lock (this)
			{
				if(_reconnecting) return;

				_reconnecting = true;
			}

			_client.Dispatcher.OnRequestThreadAsync(() =>
			{
				Disconnect();
				Connect();
			});
		}

		private void SetConnectionState(bool isConnected)
		{
			_client.VerificationCode = null;
			_reqToken = null;

			if (IsConnected == isConnected)
				return;

			_client.AddDebugLog("Etrade: {0}", isConnected ? "connected" : "disconnected");

			if (isConnected)
			{
				Api.SetAuthenticator(AccessToken);
				_reconnecting = false;
			}
			else
			{
				Api.SetAuthenticator(null);
			}

			IsConnected = isConnected;
			ConnectionStateChanged.SafeInvoke();
		}
	}
}