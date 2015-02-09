namespace StockSharp.Community
{
	using System;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Клиент для доступа к сервису авторизации StockSharp.
	/// </summary>
	public class AuthenticationClient : BaseServiceClient<IAuthenticationService>
	{
		static AuthenticationClient()
		{
			_instance = new Lazy<AuthenticationClient>(() => new AuthenticationClient());
		}

		/// <summary>
		/// Создать <see cref="AuthenticationClient"/>.
		/// </summary>
		public AuthenticationClient()
			: this(new Uri("http://stocksharp.com/services/authenticationservice.svc"))
		{
		}

		/// <summary>
		/// Создать <see cref="AuthenticationClient"/>.
		/// </summary>
		/// <param name="address">Адрес сервиса.</param>
		public AuthenticationClient(Uri address)
			: base(address, "authentication")
		{
			Credentials = new ServerCredentials();
		}

		private static readonly Lazy<AuthenticationClient> _instance;

		/// <summary>
		/// Общий клиент авторизации для всего приложения.
		/// </summary>
		public static AuthenticationClient Instance
		{
			get { return _instance.Value; }
		}

		/// <summary>
		/// Информация о логине и пароле для доступа к StockSharp.
		/// </summary>
		public ServerCredentials Credentials { get; private set; }

		/// <summary>
		/// Прошел ли успешно авторизацию клиент.
		/// </summary>
		public bool IsLoggedIn { get; private set; }

		private Guid _sessionId;

		/// <summary>
		/// Идентификатор сессии.
		/// </summary>
		public Guid SessionId
		{
			get
			{
				if (!IsLoggedIn)
					Login();

				return _sessionId;
			}
		}

		private static void RaiseException(ErrorCodes errorCode)
		{
			switch (errorCode)
			{
				case ErrorCodes.InvalidCredentials:
					throw new InvalidOperationException(LocalizedStrings.WrongLoginOrPassword);
				case ErrorCodes.TimeOut:
					throw new InvalidOperationException(LocalizedStrings.Str24);
				case ErrorCodes.Locked:
					throw new InvalidOperationException(LocalizedStrings.UserBlocked);
				case ErrorCodes.SessionNotExist:
					throw new InvalidOperationException(LocalizedStrings.SessionExpired);
				case ErrorCodes.ClientNotExist:
					throw new InvalidOperationException(LocalizedStrings.AccountNotFound);
				default:
					throw new InvalidOperationException(LocalizedStrings.UnknownServerErrorCode.Put(errorCode));
			}
		}

		/// <summary>
		/// Произвести вхов в систему.
		/// </summary>
		public void Login()
		{
			Login(Credentials.Login, Credentials.Password.To<string>());
		}

		/// <summary>
		/// Произвести вхов в систему.
		/// </summary>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		public void Login(string login, string password)
		{
			if (login.IsEmpty())
				throw new ArgumentNullException("login");

			if (password.IsEmpty())
				throw new ArgumentNullException("password");

			var sessionId = Invoke(f => f.Login(login, password));

			if (sessionId == Guid.Empty)
				throw new InvalidOperationException(LocalizedStrings.UnknownServerError);

			var bytes = sessionId.ToByteArray();
			if (bytes.Take(14).All(b => b == 0))
				RaiseException((ErrorCodes)bytes[15]);

			_sessionId = sessionId;
			IsLoggedIn = true;
		}

		/// <summary>
		/// Выйти из системы.
		/// </summary>
		public void Logout()
		{
			Invoke(f => f.Logout(SessionId));
			IsLoggedIn = false;
		}

		/// <summary>
		/// Получить идентификатор пользователя.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <returns>Идентификатор пользователя.</returns>
		public long GetId(Guid sessionId)
		{
			return Invoke(f => f.GetId(sessionId));
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (IsLoggedIn)
				Logout();

			base.DisposeManaged();
		}
	}
}