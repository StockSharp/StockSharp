namespace StockSharp.BitStamp
{
	using System.Security;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с биржей BitStamp.
	/// </summary>
	public class BitStampTrader : Connector
    {
		private readonly BitStampMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="BitStampTrader"/>.
		/// </summary>
		public BitStampTrader()
		{
			_adapter = new BitStampMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter.ToChannel(this));
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

		/// <summary>
		/// Идентификатор клиента.
		/// </summary>
		public int ClientId
		{
			get { return _adapter.ClientId; }
			set { _adapter.ClientId = value; }
		}
    }
}