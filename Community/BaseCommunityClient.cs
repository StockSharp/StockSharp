namespace StockSharp.Community
{
	using System;

	/// <summary>
	/// Базовый клиент для доступа к сервисам StockSharp.
	/// </summary>
	/// <typeparam name="TService">Тип WCF сервиса.</typeparam>
	public abstract class BaseCommunityClient<TService> : BaseServiceClient<TService>
		where TService : class
	{
		/// <summary>
		/// Инициализировать <see cref="BaseCommunityClient{TService}"/>.
		/// </summary>
		/// <param name="address">Адрес сервера.</param>
		/// <param name="endpointName">Название точки доступа в конфиг-файле.</param>
		/// <param name="hasCallbacks">Имеет ли <typeparamref name="TService"/> события.</param>
		protected BaseCommunityClient(Uri address, string endpointName, bool hasCallbacks = false)
			: base(address, endpointName, hasCallbacks)
		{
		}

		/// <summary>
		/// Идентификатор сессии, полученный из <see cref="IAuthenticationService.Login"/>.
		/// </summary>
		protected virtual Guid SessionId
		{
			get { return AuthenticationClient.Instance.SessionId; }
		}

		/// <summary>
		/// Идентификатор пользователя для <see cref="SessionId"/>.
		/// </summary>
		public long UserId
		{
			get { return AuthenticationClient.Instance.GetId(SessionId); }
		}
	}
}