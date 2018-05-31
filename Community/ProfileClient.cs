#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: ProfileClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// The client for access to the registration service.
	/// </summary>
	public class ProfileClient : BaseCommunityClient<IProfileService>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ProfileClient"/>.
		/// </summary>
		public ProfileClient()
			: this("https://stocksharp.com/services/profileservice.svc".To<Uri>())
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
		/// <param name="profile">The profile information.</param>
		public void CreateProfile(Profile profile)
		{
			ValidateError(Invoke(f => f.CreateProfile(profile)));
		}

		/// <summary>
		/// To send an e-mail message.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		public void SendEmail(string email)
		{
			ValidateError(Invoke(f => f.SendEmail(email)));
		}

		/// <summary>
		/// To confirm the e-mail address.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="emailCode">The e-mail confirmation code.</param>
		public void ValidateEmail(string email, string emailCode)
		{
			ValidateError(Invoke(f => f.ValidateEmail(email, emailCode)));
		}

		/// <summary>
		/// To send SMS.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="phone">Phone.</param>
		public void SendSms(string email, string phone)
		{
			ValidateError(Invoke(f => f.SendSms(email, phone)));
		}

		/// <summary>
		/// To confirm the phone number.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="smsCode">SMS verification code.</param>
		public void ValidatePhone(string email, string smsCode)
		{
			ValidateError(Invoke(f => f.ValidatePhone(email, smsCode)));
		}

		/// <summary>
		/// To update profile information.
		/// </summary>
		/// <param name="profile">The profile information.</param>
		public void UpdateProfile(Profile profile)
		{
			ValidateError(Invoke(f => f.UpdateProfile(SessionId, profile)));
		}

		/// <summary>
		/// To get profile information.
		/// </summary>
		/// <returns>The profile information.</returns>
		public Profile GetProfile()
		{
			return Invoke(f => f.GetProfile(SessionId));
		}

		/// <summary>
		/// To get user information.
		/// </summary>
		/// <param name="userId">User ID.</param>
		/// <returns>The user information.</returns>
		public Profile GetUserProfile(long userId)
		{
			return Invoke(f => f.GetUserProfile(userId));
		}

		private static void ValidateError(byte errorCode)
		{
			((ErrorCodes)errorCode).ThrowIfError();
		}
	}
}