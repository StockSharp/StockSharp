namespace StockSharp.Community
{
	using System;
	using System.Linq;
	using System.Threading;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Клиент для доступа к сервису уведомлений StockSharp.
	/// </summary>
	public class NotificationClient : BaseCommunityClient<INotificationService>
	{
		private Timer _newsTimer;
		private long _lastNewsId;

		/// <summary>
		/// Создать <see cref="NotificationClient"/>.
		/// </summary>
		public NotificationClient()
			: this("http://stocksharp.com/services/notificationservice.svc".To<Uri>())
		{
		}

		/// <summary>
		/// Создать <see cref="NotificationClient"/>.
		/// </summary>
		/// <param name="address">Адрес сервиса.</param>
		public NotificationClient(Uri address)
			: base(address, "notification")
		{
		}

		/// <summary>
		/// Доступное количество SMS-сообщений.
		/// </summary>
		public int SmsCount
		{
			get { return Invoke(f => f.GetSmsCount(SessionId)); }
		}

		/// <summary>
		/// Доступное количество email-сообщений.
		/// </summary>
		public int EmailCount
		{
			get { return Invoke(f => f.GetEmailCount(SessionId)); }
		}

		/// <summary>
		/// Послать SMS-сообщение.
		/// </summary>
		/// <param name="message">Тело сообщения.</param>
		public void SendSms(string message)
		{
			ValidateError(Invoke(f => f.SendSms(SessionId, message)));
		}

		/// <summary>
		/// Послать email-сообщение.
		/// </summary>
		/// <param name="caption">Заголовок сообщения.</param>
		/// <param name="message">Тело сообщения.</param>
		public void SendEmail(string caption, string message)
		{
			ValidateError(Invoke(f => f.SendEmail(SessionId, caption, message)));
		}

		/// <summary>
		/// Событие появления новости.
		/// </summary>
		public event Action<CommunityNews> NewsReceived; 

		/// <summary>
		/// Подписаться на новости.
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
		/// Отписаться от новостей.
		/// </summary>
		public void UnSubscribeNews()
		{
			if (_newsTimer != null)
				_newsTimer.Dispose();
		}

		private void RequestNews()
		{
			var news = Invoke(f => f.GetNews(Guid.Empty, _lastNewsId));

			if (news.Length <= 0)
				return;

			_lastNewsId = news.Last().Id;

			foreach (var n in news)
			{
				n.EndDate = n.EndDate.ChangeKind(DateTimeKind.Utc);
				NewsReceived.SafeInvoke(n);
			}

			if (news.Length == 100)
			{
				RequestNews();
			}
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			UnSubscribeNews();

			base.DisposeManaged();
		}

		private static void ValidateError(byte errorCode)
		{
			switch ((ErrorCodes)errorCode)
			{
				case ErrorCodes.Ok:
					return;
				case ErrorCodes.UnknownServerError:
					throw new InvalidOperationException(LocalizedStrings.UnknownServerError);

				// notify error codes
				case ErrorCodes.SmsNotEnought:
					throw new InvalidOperationException(LocalizedStrings.SmsNotEnough);
				case ErrorCodes.EmailNotEnought:
					throw new InvalidOperationException(LocalizedStrings.EmailNotEnough);
				case ErrorCodes.PhoneNotExist:
					throw new InvalidOperationException(LocalizedStrings.PhoneNotSpecified);
				default:
					throw new InvalidOperationException(LocalizedStrings.UnknownServerErrorCode.Put(errorCode));
			}
		}
	}
}