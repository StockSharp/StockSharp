namespace StockSharp.Community
{
	using System;

	using StockSharp.Community.Messages;
	using StockSharp.Messages;

	/// <summary>
	/// The interface describing a client for access to <see cref="INotificationService"/>.
	/// </summary>
	public interface INotificationClient
	{
		/// <summary>
		/// The available number of SMS-messages.
		/// </summary>
		int SmsCount { get; }

		/// <summary>
		/// The available number of email messages.
		/// </summary>
		int EmailCount { get; }

		/// <summary>
		/// To send a SMS message.
		/// </summary>
		/// <param name="message">Message body.</param>
		void SendSms(string message);

		/// <summary>
		/// To send an email message.
		/// </summary>
		/// <param name="title">The message title.</param>
		/// <param name="body">Message body.</param>
		void SendEmail(string title, string body);

		/// <summary>
		/// To send a message.
		/// </summary>
		/// <param name="title">The message title.</param>
		/// <param name="body">Message body.</param>
		/// <param name="attachments">Attachments.</param>
		void SendMessage(string title, string body, FileInfoMessage[] attachments);

		/// <summary>
		/// Send feedback for specified product.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="rating">Rating.</param>
		/// <param name="comment">Comment.</param>
		void SendFeedback(ProductInfoMessage product, int rating, string comment);

		/// <summary>
		/// Has feedback for specified product.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <returns>Check result.</returns>
		bool HasFeedback(ProductInfoMessage product);

		/// <summary>
		/// News received.
		/// </summary>
		event Action<NewsMessage> NewsReceived;

		/// <summary>
		/// To subscribe for news.
		/// </summary>
		void SubscribeNews();

		/// <summary>
		/// To unsubscribe from news.
		/// </summary>
		void UnSubscribeNews();
	}
}