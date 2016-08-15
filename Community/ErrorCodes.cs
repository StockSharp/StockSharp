#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: ErrorCodes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	enum ErrorCodes : byte
	{
		Ok,
		UnknownServerError,

		// auth service
		InvalidCredentials,
		TimeOut,
		Locked,
		SessionNotExist,
		ClientNotExist,

		// notification service
		SmsNotEnought,
		EmailNotEnought,
		PhoneNotExist,

		// license service
		LicenseRejected,
		ClientNotApproved,
		TooMuchFrequency,
		LicenseMaxRenew,

		// reg error codes
		InvalidEmail,
		InvalidPhone,
		InvalidLogin,
		InvalidPassword,
		DuplicateEmail,
		DuplicatePhone,
		DuplicateLogin,
		InvalidEmailCode,
		InvalidSmsCode,

		// strategy service
		StrategyRemoved,
		StrategyNotExist,
		StrategyPriceTypeCannotChange,
		StrategyContentTypeCannotChange,
		StrategyBacktestNotOwned,
		StrategyPrivateCannotChange,
		StrategyOwnSubscribe,
		
		TooMuchPrice,
		NotEnoughBalance,
		NotSubscribed,

		CurrencyCannotChange,

		NotCompleteRegistered,

		FileNotStarted,
		FileTooMuch,
		FileNotExist
	}
}