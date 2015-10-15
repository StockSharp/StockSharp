namespace StockSharp.Algo.Storages.Backup
{
	/// <summary>
	/// Storage element.
	/// </summary>
	public class BackupEntry
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BackupEntry"/>.
		/// </summary>
		public BackupEntry()
		{
		}

		/// <summary>
		/// Element name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Parent element.
		/// </summary>
		public BackupEntry Parent { get; set; }

		///// <summary>
		///// Является ли элемент директорией.
		///// </summary>
		//public bool IsDirectory { get; set; }

		/// <summary>
		/// Size in bytes.
		/// </summary>
		public long Size { get; set; }
	}
}