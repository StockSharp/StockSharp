#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: MemoryStatistics.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using MoreLinq;

	/// <summary>
	/// The class for tracking objects taking in memory.
	/// </summary>
	public sealed class MemoryStatistics : BaseLogReceiver
	{
		static MemoryStatistics()
		{
			Instance = new MemoryStatistics();
		}

		/// <summary>
		/// An object of class <see cref="MemoryStatistics"/>.
		/// </summary>
		public static MemoryStatistics Instance { get; }

		private readonly Timer _timer;

		private MemoryStatistics()
		{
			var lastTime = DateTime.Now;

			_timer = ThreadingHelper.Timer(() =>
			{
				if (IsDisposed)
					return;

				if (DateTime.Now - lastTime < Interval)
					return;

				lastTime = DateTime.Now;

				this.AddInfoLog(ToString());
			}).Interval(Interval);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (IsDisposed)
				return;

			_timer.Dispose();

			base.DisposeManaged();
		}

		private TimeSpan _interval = TimeSpan.FromSeconds(60);

		/// <summary>
		/// Statistics logging interval. The default is 1 minute.
		/// </summary>
		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (_interval == value)
					return;

				_interval = value;
				_timer.Change(value, value);
			}
		}

		private readonly CachedSynchronizedSet<IMemoryStatisticsValue> _values = new CachedSynchronizedSet<IMemoryStatisticsValue>();

		/// <summary>
		/// Monitored objects.
		/// </summary>
		public IList<IMemoryStatisticsValue> Values => _values;

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Interval), Interval);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Interval = storage.GetValue<TimeSpan>(nameof(Interval));
		}

		/// <summary>
		/// To clear memory statistics.
		/// </summary>
		/// <param name="resetCounter">Whether to clear the objects counter.</param>
		public void Clear(bool resetCounter)
		{
			_values.Cache.ForEach(v => v.Clear(resetCounter));
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return _values.Select(v => "{0} = {1}".Put(v.Name, v.ObjectCount)).Join(", ");
		}

		/// <summary>
		/// Is the source on.
		/// </summary>
		public static bool IsEnabled => ConfigManager.GetService<LogManager>().Sources.OfType<MemoryStatistics>().Any();

		/// <summary>
		/// To add or to remove the source <see cref="MemoryStatistics"/> from the registered <see cref="LogManager"/>.
		/// </summary>
		public static void AddOrRemove()
		{
			var sources = ConfigManager.GetService<LogManager>().Sources;

			var stat = sources.OfType<MemoryStatistics>().ToArray();

			if (stat.Any())
				sources.RemoveRange(stat);
			else
				sources.Add(Instance);
		}
	}
}