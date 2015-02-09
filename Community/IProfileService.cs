namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;
	using System.ServiceModel;

	/// <summary>
	/// Информация о профиле.
	/// </summary>
	[DataContract]
	public class Profile
	{
		/// <summary>
		/// Логин.
		/// </summary>
		[DataMember]
		public string Login { get; set; }

		/// <summary>
		/// Пароль (не заполняется при получении с сервера).
		/// </summary>
		[DataMember]
		public string Password { get; set; }

		/// <summary>
		/// Адрес электронной почты.
		/// </summary>
		[DataMember]
		public string Email { get; set; }

		/// <summary>
		/// Номер телефона.
		/// </summary>
		[DataMember]
		public string Phone { get; set; }

		/// <summary>
		/// Сайт.
		/// </summary>
		[DataMember]
		public string Homepage { get; set; }

		/// <summary>
		/// Skype.
		/// </summary>
		[DataMember]
		public string Skype { get; set; }

		/// <summary>
		/// Город.
		/// </summary>
		[DataMember]
		public string City { get; set; }

		/// <summary>
		/// Пол.
		/// </summary>
		[DataMember]
		public bool? Gender { get; set; }

		/// <summary>
		/// Включена ли рассылка.
		/// </summary>
		[DataMember]
		public bool IsSubscription { get; set; }
	}

	/// <summary>
	/// Интерфейс, описывающий сервис регистрации.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/registrationservice.svc")]
	public interface IProfileService
	{
		/// <summary>
		/// Начать процедуру регистрации.
		/// </summary>
		/// <param name="profile">Информация о профиле.</param>
		/// <returns>Код результата выполнения.</returns>
		[OperationContract]
		byte CreateProfile(Profile profile);

		/// <summary>
		/// Отправить на электронную почту письмо.
		/// </summary>
		/// <param name="email">Адрес электронной почты.</param>
		/// <param name="login">Логин.</param>
		/// <returns>Код результата выполнения.</returns>
		[OperationContract]
		byte SendEmail(string email, string login);

		/// <summary>
		/// Подтвердить адрес электронной почты.
		/// </summary>
		/// <param name="email">Адрес электронной почты.</param>
		/// <param name="login">Логин.</param>
		/// <param name="emailCode">Email код подтверждения.</param>
		/// <returns>Код результата выполнения.</returns>
		[OperationContract]
		byte ValidateEmail(string email, string login, string emailCode);

		/// <summary>
		/// Отправить SMS.
		/// </summary>
		/// <param name="email">Адрес электронной почты.</param>
		/// <param name="login">Логин.</param>
		/// <param name="phone">Номер телефона.</param>
		/// <returns>Код результата выполнения.</returns>
		[OperationContract]
		byte SendSms(string email, string login, string phone);

		/// <summary>
		/// Подтвердить телефон.
		/// </summary>
		/// <param name="email">Адрес электронной почты.</param>
		/// <param name="login">Логин.</param>
		/// <param name="smsCode">SMS код подтверждения.</param>
		/// <returns>Код результата выполнения.</returns>
		[OperationContract]
		byte ValidatePhone(string email, string login, string smsCode);

		/// <summary>
		/// Обновить данные профиля.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="profile">Информация о профиле.</param>
		/// <returns>Код результата выполнения.</returns>
		[OperationContract]
		byte UpdateProfile(Guid sessionId, Profile profile);

		/// <summary>
		/// Получить данные профиля.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Информация о профиле.</returns>
		[OperationContract]
		Profile GetProfile(Guid sessionId);

		/// <summary>
		/// Обновить фотографию профиля.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="fileName">Название файла.</param>
		/// <param name="body">Содержимое графического файла.</param>
		/// <returns>Код результата выполнения.</returns>
		[OperationContract]
		byte UpdateAvatar(Guid sessionId, string fileName, byte[] body);

		/// <summary>
		/// Получить фотографию профиля.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Содержимое графического файла.</returns>
		[OperationContract]
		byte[] GetAvatar(Guid sessionId);
	}
}