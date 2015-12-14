#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: CacheableMarketDataDrive.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Collections;

	/// <summary>
	/// The market data storage, saving data in the cache-storage.
	/// </summary>
	public class CacheableMarketDataDrive : IMarketDataStorageDrive
	{
		private readonly IMarketDataStorageDrive _cacheDrive;
		private readonly IMarketDataStorageDrive _sourceDrive;

		/// <summary>
		/// Initializes a new instance of the <see cref="CacheableMarketDataDrive"/>.
		/// </summary>
		/// <param name="sourceDrive">The initial storage of market-data.</param>
		/// <param name="cacheDrive">The cache-storage of market-data.</param>
		public CacheableMarketDataDrive(IMarketDataStorageDrive sourceDrive, IMarketDataStorageDrive cacheDrive)
		{
			if (sourceDrive == null)
				throw new ArgumentNullException(nameof(sourceDrive));

			if (cacheDrive == null)
				throw new ArgumentNullException(nameof(cacheDrive));

			_sourceDrive = sourceDrive;
			_cacheDrive = cacheDrive;
		}

		IMarketDataDrive IMarketDataStorageDrive.Drive => _sourceDrive.Drive;

		IEnumerable<DateTime> IMarketDataStorageDrive.Dates => _sourceDrive.Dates.Concat(_cacheDrive.Dates).Distinct().OrderBy();

		void IMarketDataStorageDrive.ClearDatesCache()
		{
			_cacheDrive.ClearDatesCache();
			_sourceDrive.ClearDatesCache();
		}

		void IMarketDataStorageDrive.Delete(DateTime date)
		{
			_cacheDrive.Delete(date);
			_sourceDrive.Delete(date);
		}

		void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
		{
			_sourceDrive.SaveStream(date, stream);
			_cacheDrive.SaveStream(date, stream);
		}

		Stream IMarketDataStorageDrive.LoadStream(DateTime date)
		{
			var stream = _cacheDrive.LoadStream(date);

			if (stream != Stream.Null)
				return stream;

			stream = _sourceDrive.LoadStream(date);

			if (stream == Stream.Null)
				return stream;

			_cacheDrive.SaveStream(date, stream);

			stream.Position = 0;
			return stream;
		}
	}
}