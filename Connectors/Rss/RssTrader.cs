namespace StockSharp.Rss
{
	using System;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с RSS фидами.
	/// </summary>
	public class RssTrader : Connector
    {
		private sealed class RssTransactionMessageAdapter : MessageAdapter<IMessageSessionHolder>
		{
			public RssTransactionMessageAdapter(IMessageSessionHolder sessionHolder)
				: base(MessageAdapterTypes.Transaction, sessionHolder)
			{
			}

			protected override void OnSendInMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
						SendOutMessage(new ConnectMessage());
						break;

					case MessageTypes.Disconnect:
						SendOutMessage(new DisconnectMessage());
						break;

					case MessageTypes.Time: // обработка heartbeat
						break;

					default:
						throw new NotSupportedException(LocalizedStrings.Str2143Params.Put(message.Type));
				}
			}
		}

		/// <summary>
		/// Создать <see cref="RssTrader"/>.
		/// </summary>
		public RssTrader()
		{
			base.SessionHolder = new RssSessionHolder(TransactionIdGenerator);

			TransactionAdapter = new RssTransactionMessageAdapter(SessionHolder);

			ApplyMessageProcessor(MessageDirections.In, true, false);
			ApplyMessageProcessor(MessageDirections.In, false, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		private new RssSessionHolder SessionHolder
		{
			get { return (RssSessionHolder)base.SessionHolder; }
		}

		/// <summary>
		/// Адрес RSS фида.
		/// </summary>
		public Uri Address
		{
			get { return SessionHolder.Address; }
			set { SessionHolder.Address = value; }
		}

		/// <summary>
		/// Формат дат. Необходимо заполнить, если формат RSS потока отличается от ddd, dd MMM yyyy hh:mm:ss.
		/// </summary>
		public string CustomDateFormat
		{
			get { return SessionHolder.CustomDateFormat; }
			set { SessionHolder.CustomDateFormat = value; }
		}
    }
}