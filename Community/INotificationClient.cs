namespace StockSharp.Community
{
	using System;

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
		void SendMessage(string title, string body, FileData[] attachments);

		/// <summary>
		/// Send feedback for specified product.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="rating">Rating.</param>
		/// <param name="comment">Comment.</param>
		void SendFeedback(Products product, int rating, string comment);

		/// <summary>
		/// Has feedback for specified product.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <returns>Check result.</returns>
		bool HasFeedback(Products product);

		/// <summary>
		/// News received.
		/// </summary>
		event Action<CommunityNews> NewsReceived;

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