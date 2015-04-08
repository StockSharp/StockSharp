namespace StockSharp.Btce
{
	using System.Security;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с биржей BTC-e.
	/// </summary>
	public class BtceTrader : Connector
	{
		private readonly BtceMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="BtceTrader"/>.
		/// </summary>
		public BtceTrader()
		{
			TransactionAdapter = MarketDataAdapter = _adapter = new BtceMessageAdapter(TransactionIdGenerator);

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через метод <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// в виде одной транзакции. По-умолчанию включено.
		/// </summary>
		public override bool IsSupportAtomicReRegister
		{
			get { return false; }
		}

		/// <summary>
		/// Ключ.
		/// </summary>
		public string Key
		{
			get { return _adapter.Key.To<string>(); }
			set { _adapter.Key = value.To<SecureString>(); }
		}

		/// <summary>
		/// Секрет.
		/// </summary>
		public string Secret
		{
			get { return _adapter.Secret.To<string>(); }
			set { _adapter.Secret = value.To<SecureString>(); }
		}
	}
}