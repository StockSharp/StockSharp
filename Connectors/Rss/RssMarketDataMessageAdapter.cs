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
	public class RssMarketDataMessageAdapter : MessageAdapter<RssSessionHolder>
	{
		/// <summary>
		/// Создать <see cref="RssMarketDataMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public RssMarketDataMessageAdapter(RssSessionHolder sessionHolder)
			: base(MessageAdapterTypes.MarketData, sessionHolder)
		{
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					var error = SessionHolder.Address == null
						? new InvalidOperationException(LocalizedStrings.Str3503)
						: null;

					SendOutMessage(new ConnectMessage { Error = error });

					if (error == null)
						SendInMessage(new TimeMessage());
					break;
				}

				case MessageTypes.Disconnect:
					SendOutMessage(new DisconnectMessage());
					break;

				case MessageTypes.Time: // обработка heartbeat
				{
					using (var reader = new XmlReaderEx(SessionHolder.Address.To<string>()) { CustomDateFormat = SessionHolder.CustomDateFormat })
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

					break;
				}
			}
		}
	}
}