namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

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
	}
}