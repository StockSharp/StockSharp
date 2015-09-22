namespace StockSharp.Community
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// The client for access to the registration service.
	/// </summary>
	public class ProfileClient : BaseCommunityClient<IProfileService>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProfileClient"/>.
		/// </summary>
		public ProfileClient()
			: this("http://stocksharp.com/services/registrationservice.svc".To<Uri>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProfileClient"/>.
		/// </summary>
		/// <param name="address">Service address.</param>
		public ProfileClient(Uri address)
			: base(address, "profile")
		{
		}

		/// <summary>
		/// To start the registration.
		/// </summary>
		/// <param name="profile">The profile Information.</param>
		public void CreateProfile(Profile profile)
		{
			ValidateError(Invoke(f => f.CreateProfile(profile)));
		}

		/// <summary>
		/// To send an e-mail message.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="login">Login.</param>
		public void SendEmail(string email, string login)
		{
			ValidateError(Invoke(f => f.SendEmail(email, login)));
		}

		/// <summary>
		/// To confirm the e-mail address.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="login">Login.</param>
		/// <param name="emailCode">The e-mail confirmation code.</param>
		public void ValidateEmail(string email, string login, string emailCode)
		{
			ValidateError(Invoke(f => f.ValidateEmail(email, login, emailCode)));
		}

		/// <summary>
		/// To send SMS.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="login">Login.</param>
		/// <param name="phone">Phone.</param>
		public void SendSms(string email, string login, string phone)
		{
			ValidateError(Invoke(f => f.SendSms(email, login, phone)));
		}

		/// <summary>
		/// To confirm the phone number.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="login">Login.</param>
		/// <param name="smsCode">SMS verification code.</param>
		public void ValidatePhone(string email, string login, string smsCode)
		{
			ValidateError(Invoke(f => f.ValidatePhone(email, login, smsCode)));
		}

		/// <summary>
		/// To update profile information.
		/// </summary>
		/// <param name="profile">The profile Information.</param>
		public void UpdateProfile(Profile profile)
		{
			ValidateError(Invoke(f => f.UpdateProfile(SessionId, profile)));
		}

		/// <summary>
		/// To get profile information.
		/// </summary>
		/// <returns>The profile Information.</returns>
		public Profile GetProfile()
		{
			return Invoke(f => f.GetProfile(SessionId));
		}

		/// <summary>
		/// To update the profile photo.
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <param name="body">The contents of the image file.</param>
		public void UpdateAvatar(string fileName, byte[] body)
		{
			ValidateError(Invoke(f => f.UpdateAvatar(SessionId, fileName, body)));
		}

		/// <summary>
		/// To get a profile photo.
		/// </summary>
		/// <returns>The contents of the image file.</returns>
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