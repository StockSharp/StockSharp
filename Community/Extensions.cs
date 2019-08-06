namespace StockSharp.Community
{
	using System;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Localization;

	static class Extensions
	{
		public static ErrorCodes ToErrorCode(this Guid sessionId)
		{
			if (sessionId == Guid.Empty)
				return ErrorCodes.UnknownServerError;

			var bytes = sessionId.ToByteArray();

			if (bytes.Take(14).All(b => b == 0))
				return (ErrorCodes)bytes[15];

			return ErrorCodes.Ok;
		}

		public static void ThrowIfError(this ErrorCodes code, params object[] args)
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
				
				// license error codes
				case ErrorCodes.LicenseRejected:
					throw new InvalidOperationException(LocalizedStrings.LicenseRevoked.Put(args));
				case ErrorCodes.LicenseMaxRenew:
					throw new InvalidOperationException(LocalizedStrings.LicenseMaxRenew.Put(args));
				case ErrorCodes.ClientNotApproved:
					throw new InvalidOperationException(LocalizedStrings.SmsActivationFailed);
				case ErrorCodes.TooMuchFrequency:
					throw new InvalidOperationException(LocalizedStrings.MaxLicensePerMin);

				case ErrorCodes.StrategyRemoved:
					throw new InvalidOperationException(LocalizedStrings.StrategyRemoved.Put(args));
				case ErrorCodes.StrategyNotExist:
					throw new InvalidOperationException(LocalizedStrings.StrategyNotExist.Put(args));
				case ErrorCodes.StrategyPriceTypeCannotChange:
					throw new InvalidOperationException(LocalizedStrings.StrategyPriceTypeCannotChange.Put(args));
				case ErrorCodes.StrategyContentTypeCannotChange:
					throw new InvalidOperationException(LocalizedStrings.StrategyContentTypeCannotChange.Put(args));
				case ErrorCodes.StrategyOwnSubscribe:
					throw new InvalidOperationException(LocalizedStrings.OwnStrategySubscription);
				case ErrorCodes.TooMuchPrice:
					throw new InvalidOperationException(LocalizedStrings.TooMuchPrice);
				case ErrorCodes.NotEnoughBalance:
					throw new InvalidOperationException(LocalizedStrings.NotEnoughBalance.Put(args));
				case ErrorCodes.NotSubscribed:
					throw new InvalidOperationException(LocalizedStrings.NotSubscribed.Put(args));
				case ErrorCodes.CurrencyCannotChange:
					throw new InvalidOperationException(LocalizedStrings.CurrencyCannotChange);
				case ErrorCodes.NotCompleteRegistered:
					throw new InvalidOperationException(LocalizedStrings.NotCompleteRegistered);

				case ErrorCodes.FileNotStarted:
					throw new InvalidOperationException(LocalizedStrings.FileNotStarted);
				case ErrorCodes.FileTooMuch:
					throw new InvalidOperationException(LocalizedStrings.FileTooMuch);
				case ErrorCodes.FileNotExist:
					throw new InvalidOperationException(LocalizedStrings.Str1575);

				case ErrorCodes.Suspicious:
					throw new InvalidOperationException(LocalizedStrings.SuspiciousAction);

				default:
					throw new InvalidOperationException(LocalizedStrings.UnknownServerErrorCode.Put(code));
			}
		}
	}
}