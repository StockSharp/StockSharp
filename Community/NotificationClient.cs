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
	using System.Linq;
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
			: this("https://stocksharp.com/services/notificationservice.svc".To<Uri>())
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

		private int? _smsCount;

		/// <summary>
		/// The available number of SMS-messages.
		/// </summary>
		public int SmsCount
		{
			get
			{
				if (_smsCount == null)
					_smsCount = Invoke(f => f.GetSmsCount(SessionId));

				return _smsCount.Value;
			}
			private set => _smsCount = value;
		}

		private int? _emailCount;

		/// <summary>
		/// The available number of email messages.
		/// </summary>
		public int EmailCount
		{
			get
			{
				if (_emailCount == null)
					_emailCount = Invoke(f => f.GetEmailCount(SessionId));

				return _emailCount.Value;
			}
			private set => _emailCount = value;
		}

		/// <summary>
		/// To send a SMS message.
		/// </summary>
		/// <param name="message">Message body.</param>
		public void SendSms(string message)
		{
			ValidateError(Invoke(f => f.SendSms(SessionId, message)));
			SmsCount--;
		}

		/// <summary>
		/// To send an email message.
		/// </summary>
		/// <param name="title">The message title.</param>
		/// <param name="body">Message body.</param>
		public void SendEmail(string title, string body)
		{
			ValidateError(Invoke(f => f.SendEmail(SessionId, title, body)));
			EmailCount--;
		}

		/// <summary>
		/// To send a message.
		/// </summary>
		/// <param name="title">The message title.</param>
		/// <param name="body">Message body.</param>
		/// <param name="attachments">Attachments.</param>
		public void SendMessage(string title, string body, FileData[] attachments)
		{
			if (attachments == null)
				throw new ArgumentNullException(nameof(attachments));

			ValidateError(Invoke(f => f.SendMessage(SessionId, title, body, attachments.Select(a => a.Id).ToArray(), IsEnglish)));
		}

		/// <summary>
		/// Send feedback for specified product.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="rating">Rating.</param>
		/// <param name="comment">Comment.</param>
		public void SendFeedback(Products product, int rating, string comment)
		{
			ValidateError(Invoke(f => f.SendFeedback(SessionId, product, rating, comment)));
		}

		/// <summary>
		/// Has feedback for specified product.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <returns>Check result.</returns>
		public bool HasFeedback(Products product)
		{
			return Invoke(f => f.HasFeedback(SessionId, product));
		}

		/// <summary>
		/// News received.
		/// </summary>
		public event Action<CommunityNews> NewsReceived;

		private readonly SyncObject _syncObject = new SyncObject();
		private bool _isProcessing;

		/// <summary>
		/// To subscribe for news.
		/// </summary>
		public void SubscribeNews()
		{
			_newsTimer = ThreadingHelper.Timer(() =>
			{
				lock (_syncObject)
				{
					if (_isProcessing)
						return;

					_isProcessing = true;
				}

				try
				{
					RequestNews();
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
				finally
				{
					lock (_syncObject)
						_isProcessing = false;
				}
			}).Interval(TimeSpan.Zero, TimeSpan.FromDays(1));
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
			var news = Invoke(f => f.GetNews2(NullableSessionId ?? Guid.Empty, IsEnglish, 0));

			//if (news.Length <= 0)
			//	return;

			//_lastNewsId = news.Last().Id;

			foreach (var n in news)
			{
				n.EndDate = n.EndDate.ChangeKind(DateTimeKind.Utc);
				NewsReceived?.Invoke(n);
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