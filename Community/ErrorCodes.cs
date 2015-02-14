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