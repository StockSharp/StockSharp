namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	/// <summary>
	/// Таймер, основанный на времени торговой системы.
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
		/// Создать <see cref="MarketTimer"/>.
		/// </summary>
		/// <param name="connector">Подключение к торговой системе, из которого будет использоваться событие <see cref="IConnector.MarketTimeChanged"/>.</param>
		/// <param name="activated">Обработчик таймера.</param>
		public MarketTimer(IConnector connector, Action activated)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (activated == null)
				throw new ArgumentNullException("activated");

			_connector = connector;
			_activated = activated;
		}

		/// <summary>
		/// Установить интервал.
		/// </summary>
		/// <param name="interval">Интервал таймера. Если устанавливается значение <see cref="TimeSpan.Zero"/>, то таймер перестает быть периодичным.</param>
		/// <returns>Таймер.</returns>
		public MarketTimer Interval(TimeSpan interval)
		{
			if (interval <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException("interval", interval, LocalizedStrings.Str944);

			lock (_syncLock)
			{
				_interval = interval;
				_elapsedTime = TimeSpan.Zero;
			}
			
			return this;
		}

		/// <summary>
		/// Запустить таймер.
		/// </summary>
		/// <returns>Таймер.</returns>
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
		/// Остановить таймер.
		/// </summary>
		/// <returns>Таймер.</returns>
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
		/// Освободить ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Stop();
			base.DisposeManaged();
		}
	}
}