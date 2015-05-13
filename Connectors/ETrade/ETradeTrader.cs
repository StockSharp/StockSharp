namespace StockSharp.ETrade
{
	using System;
	using System.Security;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.ETrade.Native;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с системой ETrade.
	/// </summary>
	public class ETradeTrader : Connector
	{
		/// <summary>
		/// Ключ.
		/// </summary>
		public string ConsumerKey
		{
			get { return _adapter.ConsumerKey; }
			set { _adapter.ConsumerKey = value; }
		}

		/// <summary>
		/// Секрет.
		/// </summary>
		public string ConsumerSecret
		{
			get { return _adapter.ConsumerSecret.To<string>(); }
			set { _adapter.ConsumerSecret = value.To<SecureString>(); }
		}

		/// <summary>
		/// OAuth access token. Нужен для восстановления соединения по упрощенной процедуре.
		/// Сохраненный AccessToken может быть использован до полуночи по EST.
		/// </summary>
		public OAuthToken AccessToken
		{
			get { return _adapter.AccessToken; }
			set { _adapter.AccessToken = value; }
		}

		/// <summary>
		/// Код верификации, полученный пользователем в браузере после подтверждения разрешения на работу приложения.
		/// </summary>
		public string VerificationCode
		{
			get { return _adapter.VerificationCode; }
			set { _adapter.VerificationCode = value; }
		}

		/// <summary>
		/// Режим sandbox.
		/// </summary>
		public bool Sandbox
		{
			get { return _adapter.Sandbox; }
			set { _adapter.Sandbox = value; }
		}

		private readonly ETradeMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="ETradeTrader"/>.
		/// </summary>
		public ETradeTrader()
		{
			_adapter = new ETradeMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter.ToChannel(this));

			ReConnectionSettings.TimeOutInterval = TimeSpan.FromMinutes(5);
			ReConnectionSettings.Interval = TimeSpan.FromMinutes(1);
			ReConnectionSettings.AttemptCount = 5;
		}

		/// <summary>
		/// Установить свой метод авторизации (по-умолчанию запускается браузер).
		/// </summary>
		/// <param name="method">Метод, принимающий в качестве параметра URL, по которому происходит авторизация на сайте ETrade.</param>
		public void SetCustomAuthorizationMethod(Action<string> method)
		{
			_adapter.SetCustomAuthorizationMethod(method);
		}
	}
}