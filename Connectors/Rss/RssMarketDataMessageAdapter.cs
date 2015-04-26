namespace StockSharp.Rss
{
	using System;
	using System.Linq;
	using System.ServiceModel.Syndication;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Маркет-дата адаптер сообщений для Rss.
	/// </summary>
	public partial class RssMarketDataMessageAdapter : MessageAdapter
	{
		private bool _isSubscribed;

		/// <summary>
		/// Создать <see cref="RssMarketDataMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public RssMarketDataMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			HeartbeatInterval = TimeSpan.FromMinutes(5);
			IsTransactionEnabled = false;
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_isSubscribed = false;
					SendOutMessage(new ResetMessage());
					break;
				}

				case MessageTypes.Connect:
				{
					var error = Address == null
						? new InvalidOperationException(LocalizedStrings.Str3503)
						: null;

					SendOutMessage(new ConnectMessage { Error = error });
					break;
				}

				case MessageTypes.Disconnect:
					SendOutMessage(new DisconnectMessage());
					break;

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					switch (mdMsg.DataType)
					{
						case MarketDataTypes.News:
						{
							_isSubscribed = mdMsg.IsSubscribe;
							break;
						}
						default:
						{
							SendOutMarketDataNotSupported(mdMsg.TransactionId);
							return;
						}
					}

					var reply = (MarketDataMessage)mdMsg.Clone();
					reply.OriginalTransactionId = mdMsg.TransactionId;
					SendOutMessage(reply);

					if (_isSubscribed)
						ProcessRss();

					break;
				}

				case MessageTypes.Time: // heartbeat handling
				{
					if (_isSubscribed)
						ProcessRss();

					break;
				}
			}
		}

		private void ProcessRss()
		{
			using (var reader = new XmlReaderEx(Address.To<string>()) { CustomDateFormat = CustomDateFormat })
			{
				var feed = SyndicationFeed.Load(reader);

				foreach (var item in feed.Items)
				{
					SendOutMessage(new NewsMessage
					{
						Id = item.Id,
						Source = feed.Authors.Select(a => a.Name).Join(","),
						ServerTime = item.PublishDate.DateTime,
						Headline = item.Title.Text,
						Story = item.Summary == null ? string.Empty : item.Summary.Text,
						Url = item.Links.Any() ? item.Links[0].Uri : null
					});
				}
			}
		}
	}
}