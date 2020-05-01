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

	using StockSharp.Messages;

	/// <summary>
	/// The client for access to the registration service.
	/// </summary>
	public class ProfileClient : BaseCommunityClient<IProfileService>, IProfileClient
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

		/// <inheritdoc />
		public void CreateProfile(UserInfoMessage profile)
		{
			ValidateError(Invoke(f => f.CreateProfile2(profile)));
		}

		/// <inheritdoc />
		public void SendEmail()
		{
			ValidateError(Invoke(f => f.SendEmail()));
		}

		/// <inheritdoc />
		public void ValidateEmail(string email, string emailCode)
		{
			ValidateError(Invoke(f => f.ValidateEmail(email, emailCode)));
		}

		/// <inheritdoc />
		public void SendSms(string email, string phone)
		{
			ValidateError(Invoke(f => f.SendSms(email, phone)));
		}

		/// <inheritdoc />
		public void ValidatePhone(string email, string smsCode)
		{
			ValidateError(Invoke(f => f.ValidatePhone(email, smsCode)));
		}

		/// <inheritdoc />
		public void UpdateProfile(UserInfoMessage profile)
		{
			ValidateError(Invoke(f => f.UpdateProfile2(SessionId, profile)));
		}

		/// <inheritdoc />
		public UserInfoMessage GetProfile()
		{
			return Invoke(f => f.GetProfile2(SessionId));
		}

		/// <inheritdoc />
		public UserInfoMessage GetUserProfile(long userId)
		{
			return Invoke(f => f.GetUserProfile2(userId));
		}

		private static void ValidateError(byte errorCode)
		{
			((ErrorCodes)errorCode).ThrowIfError();
		}
	}
}