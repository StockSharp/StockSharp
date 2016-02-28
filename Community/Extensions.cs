namespace StockSharp.Community
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	static class Extensions
	{
		public static void ThrowIfError(this ErrorCodes code)
		{
			switch (code)
			{
				case ErrorCodes.Ok:
					return;
				case ErrorCodes.UnknownServerError:
					throw new InvalidOperationException(LocalizedStrings.UnknownServerError);

				// auth error codes
				case ErrorCodes.InvalidCredentials:
					throw new InvalidOperationException(LocalizedStrings.WrongLoginOrPassword);
				case ErrorCodes.ClientNotExist:
					throw new InvalidOperationException(LocalizedStrings.AccountNotFound);
				case ErrorCodes.SessionNotExist:
					throw new InvalidOperationException(LocalizedStrings.SessionExpired);
				case ErrorCodes.TimeOut:
					throw new InvalidOperationException(LocalizedStrings.Str24);
				case ErrorCodes.Locked:
					throw new InvalidOperationException(LocalizedStrings.UserBlocked);

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

				// notify error codes
				case ErrorCodes.SmsNotEnought:
					throw new InvalidOperationException(LocalizedStrings.SmsNotEnough);
				case ErrorCodes.EmailNotEnought:
					throw new InvalidOperationException(LocalizedStrings.EmailNotEnough);
				case ErrorCodes.PhoneNotExist:
					throw new InvalidOperationException(LocalizedStrings.PhoneNotSpecified);

				default:
					throw new InvalidOperationException(LocalizedStrings.UnknownServerErrorCode.Put(code));
			}
		}
	}
}