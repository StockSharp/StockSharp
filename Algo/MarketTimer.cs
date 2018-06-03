#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: MarketTimer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	/// <summary>
	/// The timer, based on trading system time.
	/// </summary>
	public class MarketTimer : Disposable
	{
		private readonly IConnector _connector;
		private readonly Action _activated;
		private bool _started;
		private TimeSpan _interval;
		private readonly object _syncLock = new object();
		private TimeSpan _elapsedTime;

		/// <summary>
		/// Initializes a new instance of the <see cref="MarketTimer"/>.
		/// </summary>
		/// <param name="connector">The connection to trading system, from which event <see cref="IConnector.MarketTimeChanged"/> will be used.</param>
		/// <param name="activated">The timer processor.</param>
		public MarketTimer(IConnector connector, Action activated)
		{
			_connector = connector ?? throw new ArgumentNullException(nameof(connector));
			_activated = activated ?? throw new ArgumentNullException(nameof(activated));
		}

		/// <summary>
		/// To set the interval.
		/// </summary>
		/// <param name="interval">The timer interval. If <see cref="TimeSpan.Zero"/> value is set, timer stops to be periodical.</param>
		/// <returns>The timer.</returns>
		public MarketTimer Interval(TimeSpan interval)
		{
			if (interval <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(interval), interval, LocalizedStrings.Str944);

			lock (_syncLock)
			{
				_interval = interval;
				_elapsedTime = TimeSpan.Zero;
			}
			
			return this;
		}

		/// <summary>
		/// To start the timer.
		/// </summary>
		/// <returns>The timer.</returns>
		public MarketTimer Start()
		{
			if (_interval == default(TimeSpan))
				throw new InvalidOperationException(LocalizedStrings.Str945);

			lock (_syncLock)
			{
				if (!_started)
				{
					_started = true;
					_connector.MarketTimeChanged += OnMarketTimeChanged;
				}
			}

			return this;
		}

		/// <summary>
		/// To stop the timer.
		/// </summary>
		/// <returns>The timer.</returns>
		public MarketTimer Stop()
		{
			lock (_syncLock)
			{
				if (_started)
				{
					_started = false;
					_connector.MarketTimeChanged -= OnMarketTimeChanged;
				}	
			}
			
			return this;
		}

		private void OnMarketTimeChanged(TimeSpan diff)
		{
			lock (_syncLock)
			{
				if (!_started)
					return;

				_elapsedTime += diff;

				if (_elapsedTime < _interval)
					return;

				_elapsedTime = TimeSpan.Zero;
				_activated();
			}
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			Stop();
			base.DisposeManaged();
		}
	}
}