namespace StockSharp.Algo.Storages.Backup
{
	/// <summary>
	/// Элемент хранилища.
	/// </summary>
	public class BackupEntry
	{
		/// <summary>
		/// Создать <see cref="BackupEntry"/>.
		/// </summary>
		public BackupEntry()
		{
		}

		/// <summary>
		/// Название элемента.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Родительский элемент.
		/// </summary>
		public BackupEntry Parent { get; set; }

		///// <summary>
		///// Является ли элемент директорией.
		///// </summary>
		//public bool IsDirectory { get; set; }

		/// <summary>
		/// Размер в байтах.
		/// </summary>
		public long Size { get; set; }
	}
}