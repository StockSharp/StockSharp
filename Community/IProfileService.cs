#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: IProfileService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	using StockSharp.Messages;

	/// <summary>
	/// The interface describing the registration service.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/registrationservice.svc")]
	public interface IProfileService
	{
		/// <summary>
		/// To start the registration.
		/// </summary>
		/// <param name="profile">The profile information.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		[Obsolete]
		byte CreateProfile(Profile profile);

		/// <summary>
		/// To start the registration.
		/// </summary>
		/// <param name="profile">The profile information.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte CreateProfile2(UserInfoMessage profile);

		/// <summary>
		/// To send an e-mail message.
		/// </summary>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendEmail();

		/// <summary>
		/// To confirm the e-mail address.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="emailCode">The e-mail confirmation code.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte ValidateEmail(string email, string emailCode);

		/// <summary>
		/// To send SMS.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="phone">Phone.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendSms(string email, string phone);

		/// <summary>
		/// To confirm the phone number.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="smsCode">SMS verification code.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte ValidatePhone(string email, string smsCode);

		/// <summary>
		/// To update profile information.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="profile">The profile information.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		[Obsolete]
		byte UpdateProfile(Guid sessionId, Profile profile);

		/// <summary>
		/// To update profile information.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="profile">The profile information.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte UpdateProfile2(Guid sessionId, UserInfoMessage profile);

		/// <summary>
		/// To get profile information.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>The profile information.</returns>
		[OperationContract]
		[Obsolete]
		Profile GetProfile(Guid sessionId);

		/// <summary>
		/// To get profile information.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>The profile information.</returns>
		[OperationContract]
		UserInfoMessage GetProfile2(Guid sessionId);

		/// <summary>
		/// To get user information.
		/// </summary>
		/// <param name="userId">User ID.</param>
		/// <returns>The user information.</returns>
		[OperationContract]
		[Obsolete]
		Profile GetUserProfile(long userId);

		/// <summary>
		/// To get user information.
		/// </summary>
		/// <param name="userId">User ID.</param>
		/// <returns>The user information.</returns>
		[OperationContract]
		UserInfoMessage GetUserProfile2(long userId);
	}
}