namespace StockSharp.Community
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Клиент для доступа к сервису регистрации.
	/// </summary>
	public class ProfileClient : BaseCommunityClient<IProfileService>
	{
		/// <summary>
		/// Создать <see cref="ProfileClient"/>.
		/// </summary>
		public ProfileClient()
			: this("http://stocksharp.com/services/registrationservice.svc".To<Uri>())
		{
		}

		/// <summary>
		/// Создать <see cref="ProfileClient"/>.
		/// </summary>
		/// <param name="address">Адрес сервиса.</param>
		public ProfileClient(Uri address)
			: base(address, "profile")
		{
		}

		/// <summary>
		/// Начать процедуру регистрации.
		/// </summary>
		/// <param name="profile">Информация о профиле.</param>
		public void CreateProfile(Profile profile)
		{
			ValidateError(Invoke(f => f.CreateProfile(profile)));
		}

		/// <summary>
		/// Отправить на электронную почту письмо.
		/// </summary>
		/// <param name="email">Адрес электронной почты.</param>
		/// <param name="login">Логин.</param>
		public void SendEmail(string email, string login)
		{
			ValidateError(Invoke(f => f.SendEmail(email, login)));
		}

		/// <summary>
		/// Подтвердить адрес электронной почты.
		/// </summary>
		/// <param name="email">Адрес электронной почты.</param>
		/// <param name="login">Логин.</param>
		/// <param name="emailCode">Email код подтверждения.</param>
		public void ValidateEmail(string email, string login, string emailCode)
		{
			ValidateError(Invoke(f => f.ValidateEmail(email, login, emailCode)));
		}

		/// <summary>
		/// Отправить SMS.
		/// </summary>
		/// <param name="email">Адрес электронной почты.</param>
		/// <param name="login">Логин.</param>
		/// <param name="phone">Номер телефона.</param>
		public void SendSms(string email, string login, string phone)
		{
			ValidateError(Invoke(f => f.SendSms(email, login, phone)));
		}

		/// <summary>
		/// Подтвердить телефон.
		/// </summary>
		/// <param name="email">Адрес электронной почты.</param>
		/// <param name="login">Логин.</param>
		/// <param name="smsCode">SMS код подтверждения.</param>
		public void ValidatePhone(string email, string login, string smsCode)
		{
			ValidateError(Invoke(f => f.ValidatePhone(email, login, smsCode)));
		}

		/// <summary>
		/// Обновить данные профиля.
		/// </summary>
		/// <param name="profile">Информация о профиле.</param>
		public void UpdateProfile(Profile profile)
		{
			ValidateError(Invoke(f => f.UpdateProfile(SessionId, profile)));
		}

		/// <summary>
		/// Получить данные профиля.
		/// </summary>
		/// <returns>Информация о профиле.</returns>
		public Profile GetProfile()
		{
			return Invoke(f => f.GetProfile(SessionId));
		}

		/// <summary>
		/// Обновить фотографию профиля.
		/// </summary>
		/// <param name="fileName">Название файла.</param>
		/// <param name="body">Содержимое графического файла.</param>
		public void UpdateAvatar(string fileName, byte[] body)
		{
			ValidateError(Invoke(f => f.UpdateAvatar(SessionId, fileName, body)));
		}

		/// <summary>
		/// Получить фотографию профиля.
		/// </summary>
		/// <returns>Содержимое графического файла.</returns>
		public byte[] GetAvatar()
		{
			return Invoke(f => f.GetAvatar(SessionId));
		}

		private static void ValidateError(byte errorCode)
		{
			switch ((ErrorCodes)errorCode)
			{
				case ErrorCodes.Ok:
					return;
				case ErrorCodes.UnknownServerError:
					throw new InvalidOperationException(LocalizedStrings.UnknownServerError);

				// auth error codes
				case ErrorCodes.ClientNotExist:
					throw new InvalidOperationException(LocalizedStrings.AccountNotFound);
				case ErrorCodes.SessionNotExist:
					throw new InvalidOperationException(LocalizedStrings.SessionExpired);

				// reg error codes
				case ErrorCodes.InvalidEmail:
					throw new InvalidOperationException(LocalizedStrings.EmailIncorrect);
				case ErrorCodes.InvalidLogin:
					throw new InvalidOperationException(LocalizedStrings.LoginIncorrect);
				case ErrorCodes.InvalidPhone:
					throw new InvalidOperationException(LocalizedStrings.PhoneIncorrect);
				case ErrorCodes.InvalidPassword:
					throw new InvalidOperationException(LocalizedStrings.PasswordNotCriteria);
				case ErrorCodes.DuplicateEmail:
					throw new InvalidOperationException(LocalizedStrings.EmailAlreadyUse);
				case ErrorCodes.DuplicatePhone:
					throw new InvalidOperationException(LocalizedStrings.PhoneAlreadyUse);
				case ErrorCodes.DuplicateLogin:
					throw new InvalidOperationException(LocalizedStrings.LoginAlreadyUse);
				case ErrorCodes.InvalidEmailCode:
					throw new InvalidOperationException(LocalizedStrings.IncorrectVerificationCode);
				case ErrorCodes.InvalidSmsCode:
					throw new InvalidOperationException(LocalizedStrings.IncorrectSmsCode);
				default:
					throw new InvalidOperationException(LocalizedStrings.UnknownServerErrorCode.Put(errorCode));
			}
		}
	}
}