namespace StockSharp.Algo.Storages.Backup
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;

	/// <summary>
	/// Интерфейс, описывающий интернет-сервис хранения данных.
	/// </summary>
	public interface IBackupService
	{
		/// <summary>
		/// Получить список файлов в сервисе.
		/// </summary>
		/// <param name="parent">Элемент.</param>
		/// <returns>Список файлов.</returns>
		IEnumerable<BackupEntry> Get(BackupEntry parent);

		/// <summary>
		/// Удалить файл из сервиса.
		/// </summary>
		/// <param name="entry">Элемент.</param>
		void Delete(BackupEntry entry);

		/// <summary>
		/// Сохранить файл.
		/// </summary>
		/// <param name="entry">Элемент.</param>
		/// <param name="stream">Поток открытого файла, который будет сохранен в сервисе.</param>
		/// <param name="progress">Оповещение о прогрессе.</param>
		/// <returns>Токен отмены.</returns>
		CancellationTokenSource Download(BackupEntry entry, Stream stream, Action<int> progress);

		/// <summary>
		/// Загрузить файл.
		/// </summary>
		/// <param name="entry">Элемент.</param>
		/// <param name="stream">Поток открытого файла, в который будет скачены данные из сервиса.</param>
		/// <param name="progress">Оповещение о прогрессе.</param>
		/// <returns>Токен отмены.</returns>
		CancellationTokenSource Upload(BackupEntry entry, Stream stream, Action<int> progress);
	}
}