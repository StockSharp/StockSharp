namespace StockSharp.Community
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Клиент для доступа к сервису работы с файлами и документами.
	/// </summary>
	public class FileClient : BaseCommunityClient<IFileService>
	{
		/// <summary>
		/// Создать <see cref="FileClient"/>.
		/// </summary>
		public FileClient()
			: this("http://stocksharp.com/services/fileservice.svc".To<Uri>())
		{
		}

		/// <summary>
		/// Создать <see cref="FileClient"/>.
		/// </summary>
		/// <param name="address">Адрес сервиса.</param>
		public FileClient(Uri address)
			: base(address, "file")
		{
		}

		/// <summary>
		/// Выложить на сайт файл.
		/// </summary>
		/// <param name="fileName">Имя файла.</param>
		/// <param name="body">Тело файла.</param>
		/// <returns>Ссылка на выложенный файл.</returns>
		public string Upload(string fileName, byte[] body)
		{
			return Invoke(f => f.Upload(SessionId, fileName, body));
		}
	}
}