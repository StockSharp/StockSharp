namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;
	using System.ServiceModel;

	/// <summary>
	/// Продукты.
	/// </summary>
	[DataContract]
	public enum Products
	{
		/// <summary>
		/// S#.API
		/// </summary>
		[EnumMember]
		Api,

		/// <summary>
		/// S#.Data
		/// </summary>
		[EnumMember]
		Hydra,

		/// <summary>
		/// S#.Studio
		/// </summary>
		[EnumMember]
		Studio,

		/// <summary>
		/// S#.Server
		/// </summary>
		[EnumMember]
		Server,

		/// <summary>
		/// S#.StrategyRunner
		/// </summary>
		[EnumMember]
		StrategyRunner
	}

	/// <summary>
	/// Интерфейс, описывающий сервис документации.
	/// </summary>
	[ServiceContract]
	public interface IDocService
	{
		/// <summary>
		/// Получить дочерние страницы.
		/// </summary>
		/// <param name="parentUrl">Строка запроса родительской страницы.</param>
		/// <returns>Дочерние страницы. Если страниц нет, то будет возвращено null.</returns>
		[OperationContract]
		DocPage[] GetChildPages(string parentUrl);

		/// <summary>
		/// Получить тело страницы.
		/// </summary>
		/// <param name="url">Строка запроса страницы.</param>
		/// <returns>Тело страницы.</returns>
		[OperationContract]
		string GetContentBody(string url);

		/// <summary>
		/// Загрузить новую документацию.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="pages">Новые страницы документации.</param>
		[OperationContract]
		void Upload(Guid sessionId, DocPage[] pages);

		/// <summary>
		/// Загрузить описание новой версии.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="version">Номер новой версии.</param>
		/// <param name="product">Тип продукта.</param>
		/// <param name="description">Описание новой версии.</param>
		[OperationContract]
		void PostNewVersion(Guid sessionId, Products product, string version, string description);
	}
}