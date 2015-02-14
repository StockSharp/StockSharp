namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// Интерфейс, описывающий сервис работы с файлами и документами.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/fileservice.svc")]
	public interface IFileService
	{
		/// <summary>
		/// Выложить на сайт файл.
		/// </summary>
		/// <param name="sessionId">Идентификатор сессии.</param>
		/// <param name="fileName">Имя файла.</param>
		/// <param name="body">Тело файла.</param>
		/// <returns>Ссылка на выложенный файл.</returns>
		[OperationContract]
		string Upload(Guid sessionId, string fileName, byte[] body);
	}
}