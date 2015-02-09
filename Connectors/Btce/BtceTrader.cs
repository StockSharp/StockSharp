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
		/// <summary>
		/// Создать <see cref="BtceTrader"/>.
		/// </summary>
		public BtceTrader()
		{
			base.SessionHolder = new BtceSessionHolder(TransactionIdGenerator);

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		private new BtceSessionHolder SessionHolder
		{
			get { return (BtceSessionHolder)base.SessionHolder; }
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
			get { return SessionHolder.Key.To<string>(); }
			set { SessionHolder.Key = value.To<SecureString>(); }
		}

		/// <summary>
		/// Секрет.
		/// </summary>
		public string Secret
		{
			get { return SessionHolder.Secret.To<string>(); }
			set { SessionHolder.Secret = value.To<SecureString>(); }
		}
	}
}