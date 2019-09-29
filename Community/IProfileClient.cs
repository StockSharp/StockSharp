namespace StockSharp.Community
{
	/// <summary>
	/// The interface describing a client for access to the registration service.
	/// </summary>
	public interface IProfileClient
	{
		/// <summary>
		/// To start the registration.
		/// </summary>
		/// <param name="profile">The profile information.</param>
		void CreateProfile(Profile profile);

		/// <summary>
		/// To send an e-mail message.
		/// </summary>
		void SendEmail();

		/// <summary>
		/// To confirm the e-mail address.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="emailCode">The e-mail confirmation code.</param>
		void ValidateEmail(string email, string emailCode);

		/// <summary>
		/// To send SMS.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="phone">Phone.</param>
		void SendSms(string email, string phone);

		/// <summary>
		/// To confirm the phone number.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="smsCode">SMS verification code.</param>
		void ValidatePhone(string email, string smsCode);

		/// <summary>
		/// To update profile information.
		/// </summary>
		/// <param name="profile">The profile information.</param>
		void UpdateProfile(Profile profile);

		/// <summary>
		/// To get profile information.
		/// </summary>
		/// <returns>The profile information.</returns>
		Profile GetProfile();

		/// <summary>
		/// To get user information.
		/// </summary>
		/// <param name="userId">User ID.</param>
		/// <returns>The user information.</returns>
		Profile GetUserProfile(long userId);
	}
}