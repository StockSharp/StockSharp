namespace StockSharp.Algo.Storages
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Collections;

	/// <summary>
	/// ’ранилище маркет-данных, сохран€ющее данных в кэш-хранилище.
	/// </summary>
	public class CacheableMarketDataDrive : IMarketDataStorageDrive
	{
		private readonly IMarketDataStorageDrive _cacheDrive;
		private readonly IMarketDataStorageDrive _sourceDrive;

		/// <summary>
		/// —оздать <see cref="CacheableMarketDataDrive"/>.
		/// </summary>
		/// <param name="sourceDrive">»сходное хранилище маркет-данных.</param>
		/// <param name="cacheDrive"> эш-хранилище маркет-данных.</param>
		public CacheableMarketDataDrive(IMarketDataStorageDrive sourceDrive, IMarketDataStorageDrive cacheDrive)
		{
			if (sourceDrive == null)
				throw new ArgumentNullException("sourceDrive");

			if (cacheDrive == null)
				throw new ArgumentNullException("cacheDrive");

			_sourceDrive = sourceDrive;
			_cacheDrive = cacheDrive;
		}

		IMarketDataDrive IMarketDataStorageDrive.Drive
		{
			get { return _sourceDrive.Drive; }
		}

		IEnumerable<DateTime> IMarketDataStorageDrive.Dates
		{
			get { return _sourceDrive.Dates.Concat(_cacheDrive.Dates).Distinct().OrderBy(); }
		}

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