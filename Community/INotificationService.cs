#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: INotificationService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// The interface to the notification sending service to the phone or e-mail.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/notificationservice.svc")]
	public interface INotificationService
	{
		/// <summary>
		/// To get the available number of SMS messages.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>The available number of SMS-messages.</returns>
		[OperationContract]
		int GetSmsCount(Guid sessionId);

		/// <summary>
		/// To get the available number of email messages.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>The available number of email messages.</returns>
		[OperationContract]
		int GetEmailCount(Guid sessionId);

		/// <summary>
		/// To send a SMS message.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="message">Message body.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendSms(Guid sessionId, string message);

		/// <summary>
		/// To send an email message.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="title">The message title.</param>
		/// <param name="body">Message body.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendEmail(Guid sessionId, string title, string body);

		/// <summary>
		/// To send a message.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="title">The message title.</param>
		/// <param name="body">Message body.</param>
		/// <param name="attachments">Attachments.</param>
		/// <param name="isEnglish">Message in English.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendMessage(Guid sessionId, string title, string body, long[] attachments, bool isEnglish);

		///// <summary>
		///// To get the latest news.
		///// </summary>
		///// <param name="sessionId">Session ID. It can be empty if the request is anonymous.</param>
		///// <param name="fromId">The identifier from which you need to receive the news.</param>
		///// <returns>Last news.</returns>
		//[OperationContract]
		//CommunityNews[] GetNews(Guid sessionId, long fromId);

		/// <summary>
		/// To get the latest news.
		/// </summary>
		/// <param name="sessionId">Session ID. It can be empty if the request is anonymous.</param>
		/// <param name="isEnglish">Request news on English.</param>
		/// <param name="fromId">The identifier from which you need to receive the news.</param>
		/// <returns>Last news.</returns>
		[OperationContract]
		CommunityNews[] GetNews2(Guid sessionId, bool isEnglish, long fromId);

		/// <summary>
		/// Has feedback for specified product.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="product">Product.</param>
		/// <returns>Check result.</returns>
		[OperationContract]
		bool HasFeedback(Guid sessionId, Products product);

		/// <summary>
		/// Send feedback for specified product.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="product">Product.</param>
		/// <param name="rating">Rating.</param>
		/// <param name="comment">Comment.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendFeedback(Guid sessionId, Products product, int rating, string comment);
	}
}