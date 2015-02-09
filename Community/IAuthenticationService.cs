namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// Интерфейс, описывающий сервис авторизации.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/authenticationservice.svc")]
	public interface IAuthenticationService
	{
		/// <summary>
		/// Произвести вхов в систему.
		/// </summary>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		/// <returns>Идентификатор сессии.</returns>
		[OperationContract]
		Guid Login(string login, string password);

		/// <summary>
		/// Выйти из системы.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		[OperationContract]
		void Logout(Guid sessionId);

		/// <summary>
		/// Получить идентификатор пользователя.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Идентификатор пользователя.</returns>
		[OperationContract]
		long GetId(Guid sessionId);
	}
}