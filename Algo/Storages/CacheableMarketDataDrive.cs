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
	using Ecng.Common;

	/// <summary>
	/// The market data storage, saving data in the cache-storage.
	/// </summary>
	public class CacheableMarketDataDrive : IMarketDataStorageDrive
	{
		private readonly IMarketDataStorageDrive _cacheDrive;
		private readonly Action<Exception> _errorHandler;
		private readonly IMarketDataStorageDrive _sourceDrive;

		/// <summary>
		/// Initializes a new instance of the <see cref="CacheableMarketDataDrive"/>.
		/// </summary>
		/// <param name="drive">The storage (database, file etc.).</param>
		/// <param name="sourceDrive">The initial storage of market-data.</param>
		/// <param name="cacheDrive">The cache-storage of market-data.</param>
		/// <param name="errorHandler">Error handler.</param>
		public CacheableMarketDataDrive(IMarketDataDrive drive, IMarketDataStorageDrive sourceDrive, IMarketDataStorageDrive cacheDrive, Action<Exception> errorHandler)
		{
			_drive = drive ?? throw new ArgumentNullException(nameof(drive));
			_sourceDrive = sourceDrive ?? throw new ArgumentNullException(nameof(sourceDrive));
			_cacheDrive = cacheDrive ?? throw new ArgumentNullException(nameof(cacheDrive));
			_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		}

		private void SafeDo(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			SafeDo<object>(() =>
			{
				action();
				return null;
			}, null);
		}

		private static readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
		private DateTime? _lastErrorTime;

		private T SafeDo<T>(Func<T> func, T defaultValue, bool delay = false)
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));

			try
			{
				if (delay && _lastErrorTime != null && (_lastErrorTime.Value + _interval) < TimeHelper.Now)
					return defaultValue;

				var value = func();
				_lastErrorTime = null;
				return value;
			}
			catch (Exception ex)
			{
				_lastErrorTime = TimeHelper.Now;
				_errorHandler(ex);
				return defaultValue;
			}
		}

		private readonly IMarketDataDrive _drive;
		IMarketDataDrive IMarketDataStorageDrive.Drive => _drive;

		private IEnumerable<DateTime> _prevSourceDates = Enumerable.Empty<DateTime>();

		IEnumerable<DateTime> IMarketDataStorageDrive.Dates
		{
			get
			{
				_prevSourceDates = SafeDo(() => _sourceDrive.Dates, _prevSourceDates, true);

				return _prevSourceDates
				       .Concat(_cacheDrive.Dates)
				       .Distinct()
				       .OrderBy();
			}
		}

		void IMarketDataStorageDrive.ClearDatesCache()
		{
			_cacheDrive.ClearDatesCache();
			SafeDo(_sourceDrive.ClearDatesCache);
		}

		void IMarketDataStorageDrive.Delete(DateTime date)
		{
			_cacheDrive.Delete(date);
			SafeDo(() => _sourceDrive.Delete(date));
		}

		void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
		{
			_cacheDrive.SaveStream(date, stream);
			SafeDo(() => _sourceDrive.SaveStream(date, stream));
		}

		Stream IMarketDataStorageDrive.LoadStream(DateTime date)
		{
			var stream = _cacheDrive.LoadStream(date);

			if (stream != Stream.Null)
				return stream;

			stream = SafeDo(() => _sourceDrive.LoadStream(date), Stream.Null);
			
			if (stream == Stream.Null)
				return stream;

			_cacheDrive.SaveStream(date, stream);

			stream.Position = 0;
			return stream;
		}
	}
}