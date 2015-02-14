namespace StockSharp.Community
{
	using System;

	using Ecng.Security;

	using StockSharp.Localization;

	/// <summary>
	/// Модуль проверки доступа соединения, основанный на <see cref="IAuthenticationService"/> авторизации.
	/// </summary>
	public class CommunityAuthorization : IAuthorization
	{
		/// <summary>
		/// Создать <see cref="CommunityAuthorization"/>.
		/// </summary>
		public CommunityAuthorization()
		{
		}

		/// <summary>
		/// Проверить логин и пароль на правильность.
		/// </summary>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		/// <returns>Идентификатор сессии.</returns>
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