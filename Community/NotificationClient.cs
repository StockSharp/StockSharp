namespace StockSharp.Community
{
	using System;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Клиент для доступа к сервису уведомлений StockSharp.
	/// </summary>
	public class NotificationClient : BaseCommunityClient<INotificationService>
	{
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
		/// Получить последние новости.
		/// </summary>
		/// <param name="fromId">Идентификатор, с которого необходимо получить новости.</param>
		/// <returns>Последние новости.</returns>
		public Tuple<long, string, string, int>[] GetNews(long fromId)
		{
			var news = Invoke(f => f.GetNews(SessionId, fromId));

			if (news.Length == 100)
				news = news.Concat(GetNews(news.Last().Item1));

			return news;
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