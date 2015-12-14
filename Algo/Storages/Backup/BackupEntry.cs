#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Backup.Algo
File: BackupEntry.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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