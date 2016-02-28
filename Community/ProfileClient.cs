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
			((ErrorCodes)errorCode).ThrowIfError();
		}
	}
}