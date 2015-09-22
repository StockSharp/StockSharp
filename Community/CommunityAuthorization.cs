namespace StockSharp.Community
{
	using System;

	using Ecng.Security;

	using StockSharp.Localization;

	/// <summary>
	/// The module of the connection access check based on the <see cref="IAuthenticationService"/> authorization.
	/// </summary>
	public class CommunityAuthorization : IAuthorization
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommunityAuthorization"/>.
		/// </summary>
		public CommunityAuthorization()
		{
		}

		/// <summary>
		/// To check the username and password on correctness.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <returns>Session ID.</returns>
		public virtual Guid ValidateCredentials(string login, string password)
		{
			try
			{
				AuthenticationClient.Instance.Login(login, password);
				return Guid.NewGuid();
			}
			catch (Exception ex)
			{
				throw new UnauthorizedAccessException(LocalizedStrings.WrongLoginOrPassword, ex);
			}
		}
	}
}