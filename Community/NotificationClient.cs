#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: NotificationClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.Threading;

	using Ecng.Common;

	using StockSharp.Logging;

	/// <summary>
	/// The client for access to the StockSharp notification service.
	/// </summary>
	public class NotificationClient : BaseCommunityClient<INotificationService>
	{
		private Timer _newsTimer;
		//private long _lastNewsId;

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationClient"/>.
		/// </summary>
		public NotificationClient()
			: this("http://stocksharp.com/services/notificationservice.svc".To<Uri>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationClient"/>.
		/// </summary>
		/// <param name="address">Service address.</param>
		public NotificationClient(Uri address)
			: base(address, "notification")
		{
		}

		/// <summary>
		/// The available number of SMS-messages.
		/// </summary>
		public int SmsCount
		{
			get { return Invoke(f => f.GetSmsCount(SessionId)); }
		}

		/// <summary>
		/// The available number of email messages.
		/// </summary>
		public int EmailCount
		{
			get { return Invoke(f => f.GetEmailCount(SessionId)); }
		}

		/// <summary>
		/// To send a SMS message.
		/// </summary>
		/// <param name="message">Message body.</param>
		public void SendSms(string message)
		{
			ValidateError(Invoke(f => f.SendSms(SessionId, message)));
		}

		/// <summary>
		/// To send an email message.
		/// </summary>
		/// <param name="caption">The message title.</param>
		/// <param name="message">Message body.</param>
		public void SendEmail(string caption, string message)
		{
			ValidateError(Invoke(f => f.SendEmail(SessionId, caption, message)));
		}

		/// <summary>
		/// News received.
		/// </summary>
		public event Action<CommunityNews> NewsReceived; 

		/// <summary>
		/// To subscribe for news.
		/// </summary>
		public void SubscribeNews()
		{
			RequestNews();
			_newsTimer = ThreadingHelper.Timer(() =>
			{
				try
				{
					RequestNews();
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}).Interval(TimeSpan.FromDays(1));
		}

		/// <summary>
		/// To unsubscribe from news.
		/// </summary>
		public void UnSubscribeNews()
		{
			_newsTimer?.Dispose();
		}

		private void RequestNews()
		{
			var news = Invoke(f => f.GetNews(Guid.Empty, 0));

			//if (news.Length <= 0)
			//	return;

			//_lastNewsId = news.Last().Id;

			foreach (var n in news)
			{
				n.EndDate = n.EndDate.ChangeKind(DateTimeKind.Utc);
				NewsReceived.SafeInvoke(n);
			}

			//if (news.Length == 100)
			//{
			//	RequestNews();
			//}
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			UnSubscribeNews();

			base.DisposeManaged();
		}

		private static void ValidateError(byte errorCode)
		{
			((ErrorCodes)errorCode).ThrowIfError();
		}
	}
}