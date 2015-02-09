namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	using Ecng.Common;
	using Ecng.Net;

	/// <summary>
	/// Базовый клиент для доступа к WCFсервисам.
	/// </summary>
	/// <typeparam name="TService">Тип WCF сервиса.</typeparam>
	public abstract class BaseServiceClient<TService> : Disposable
		where TService : class
	{
		private readonly string _endpointName;
		private readonly bool _hasCallbacks;
		private TService _service;
		private ChannelFactory<TService> _factory;

		/// <summary>
		/// Инициализировать <see cref="BaseCommunityClient{TService}"/>.
		/// </summary>
		/// <param name="address">Адрес сервера.</param>
		/// <param name="endpointName">Название точки доступа в конфиг-файле.</param>
		/// <param name="hasCallbacks">Имеет ли <typeparamref name="TService"/> события.</param>
		protected BaseServiceClient(Uri address, string endpointName, bool hasCallbacks = false)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			Address = address;
			_endpointName = endpointName;
			_hasCallbacks = hasCallbacks;
		}

		/// <summary>
		/// Адрес сервера.
		/// </summary>
		public Uri Address { get; private set; }

		/// <summary>
		/// Было ли установлено подключение.
		/// </summary>
		public bool IsConnected { get; private set; }

		/// <summary>
		/// Создать WCF канал.
		/// </summary>
		/// <returns>WCF канал.</returns>
		protected virtual ChannelFactory<TService> CreateChannel()
		{
			return new ChannelFactory<TService>(new WSHttpBinding { Security = { Mode = SecurityMode.None }, UseDefaultWebProxy = true }, new EndpointAddress(Address));
		}

		/// <summary>
		/// Подключиться. Соединение устанавливается автоматически при обращении к методам <see cref="Invoke"/> или <see cref="Invoke{TResult}"/>.
		/// </summary>
		public virtual void Connect()
		{
			_factory = ChannelHelper.Create(_endpointName, CreateChannel);

			if (_hasCallbacks)
				_service = _factory.CreateChannel();

			OnConnect();

			IsConnected = true;
		}

		/// <summary>
		/// Подключиться.
		/// </summary>
		protected virtual void OnConnect()
		{
		}

		/// <summary>
		/// Вызвать метод сервиса <typeparamref name="TService"/>.
		/// </summary>
		/// <typeparam name="TResult">Тип результата, возвращающий метод сервиса.</typeparam>
		/// <param name="handler">Обработчик, в котором вызывает метод.</param>
		/// <returns>Результат, возвращающий метод сервиса.</returns>
		protected virtual TResult Invoke<TResult>(Func<TService, TResult> handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			if (!IsConnected)
				Connect();

			return _hasCallbacks ? handler(_service) : _factory.Invoke(handler);
		}

		/// <summary>
		/// Вызвать метод сервиса <typeparamref name="TService"/>.
		/// </summary>
		/// <param name="handler">Обработчик, в котором вызывает метод.</param>
		protected void Invoke(Action<TService> handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			Invoke<object>(srv =>
			{
				handler(srv);
				return null;
			});
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (IsConnected)
			{
				((IDisposable)_factory).Dispose();

				_factory = null;
				_service = null;

				IsConnected = false;
			}

			base.DisposeManaged();
		}
	}
}