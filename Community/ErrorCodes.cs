#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: ErrorCodes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	enum ErrorCodes : byte
	{
		Ok = 0,
		UnknownServerError = 1,

		// auth service
		InvalidCredentials = 1,
		TimeOut = 4,
		Locked = 8,
		SessionNotExist = 10,
		ClientNotExist = 11,

		// notification service
		SmsNotEnought = 12,
		EmailNotEnought = 13,
		PhoneNotExist = 14,

		// license service
		LicenseRejected = 6,
		ClientNotApproved = 9,
		TooMuchFrequency = 12,
		LicenseMaxRenew = 7,

		// reg error codes
		InvalidEmail = 2,
		InvalidPhone = 3,
		InvalidLogin = 4,
		InvalidPassword = 5,
		DuplicateEmail = 6,
		DuplicatePhone = 7,
		DuplicateLogin = 8,
		InvalidEmailCode = 9,
		InvalidSmsCode = 12,
	}
}