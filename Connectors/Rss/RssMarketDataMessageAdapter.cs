#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rss.Rss
File: RssMarketDataMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// The market-data message adapter for Rss.
	/// </summary>
	public partial class RssMarketDataMessageAdapter : MessageAdapter
	{
		private bool _isSubscribed;

		/// <summary>
		/// Initializes a new instance of the <see cref="RssMarketDataMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public RssMarketDataMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			HeartbeatInterval = TimeSpan.FromMinutes(5);
			
			this.AddSupportedMessage(MessageTypes.MarketData);
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
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
						ServerTime = item.PublishDate,
						Headline = item.Title.Text,
						Story = item.Summary == null ? string.Empty : item.Summary.Text,
						Url = item.Links.Any() ? item.Links[0].Uri : null
					});
				}
			}
		}
	}
}