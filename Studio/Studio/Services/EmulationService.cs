#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Services.StudioPublic
File: EmulationService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Services
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Testing;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	public class EmulationService : NotifiableObject, IPersistable
	{
		private readonly BatchEmulation _basketEmulation;

		private Session _emulationSession;
		private StrategyInfo _infoClone;

		private DateTime _prevSystemTime;
		private DateTimeOffset _prevEmulTime;
		private TimeSpan _emulDuration;

		public StrategyContainer Strategy { get; }

		public EmulationSettings EmulationSettings => _basketEmulation.EmulationSettings;

		public HistoryEmulationConnector EmulationConnector => _basketEmulation.EmulationConnector;

		public IEnumerable<Strategy> Strategies { get; set; }

		public bool CanStart { get; private set; }

		public bool CanStop { get; private set; }

		public event Action<StrategyContainer, int> ProgressChanged;

		#region Статистика

		private bool _isInProgress;

		public bool IsInProgress
		{
			get { return _isInProgress; }
			set
			{
				_isInProgress = value;
				NotifyPropertyChanged("IsInProgress");
			}
		}

		private int _progress;

		public int Progress
		{
			get { return _progress; }
			set
			{
				_progress = value;
				NotifyPropertyChanged("Progress");
			}
		}

		private DateTimeOffset _marketTime;

		public DateTimeOffset MarketTime
		{
			get { return _marketTime; }
			set
			{
				_marketTime = value;
				NotifyPropertyChanged("MarketTime");
			}
		}

		private int _errorCount;

		public int ErrorCount
		{
			get { return _errorCount; }
			set
			{
				_errorCount = value;
				NotifyPropertyChanged("ErrorCount");
			}
		}

		private TimeSpan _duration;
		
		public TimeSpan Duration
		{
			get { return _duration; }
			set
			{
				_duration = value;
				NotifyPropertyChanged("Duration");
			}
		}

		private TimeSpan _remaining;
		
		public TimeSpan Remaining
		{
			get { return _remaining; }
			set
			{
				_remaining = value;
				NotifyPropertyChanged("Remaining");
			}
		}

		#endregion

		public EmulationService(StrategyContainer strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			Strategy = strategy;
			Strategies = new[] { strategy };

			var storageRegistry = new StudioStorageRegistry { MarketDataSettings = Strategy.MarketDataSettings };

			_basketEmulation = new BatchEmulation(new StudioSecurityProvider(), new Portfolio[0], storageRegistry);

			_basketEmulation.StateChanged += BasketEmulationOnStateChanged;
			_basketEmulation.ProgressChanged += (curr, total) =>
			{
				Progress = total;

				_basketEmulation
					.BatchStrategies
					.OfType<StrategyContainer>()
					.ForEach(s => ProgressChanged.SafeInvoke((StrategyContainer)s.Strategy, curr));
			};

			ConfigManager.GetService<LogManager>().Sources.Add(EmulationConnector);

			EmulationConnector.HistoryMessageAdapter.StorageRegistry = storageRegistry;

			CanStart = true;
		}

		public void StartEmulation()
		{
			if (EmulationConnector.State == EmulationStates.Suspended)
			{
				EmulationConnector.Start();
			}
			else
			{
				if (!CanStart)
					return;

				CanStart = false;

				CreateEmulationSession();

				_basketEmulation.Start(GetStrategies());
			}
		}

		public void PauseEmulation()
		{
			EmulationConnector.Suspend();
		}

		public void StopEmulation()
		{
			if (!CanStop)
				return;

			CanStop = false;

			//if (!Check(CanStop))
			//	return;

			_basketEmulation.Stop();
		}

		private IEnumerable<Strategy> GetStrategies()
		{
			EmulationConnector.AddInfoLog(LocalizedStrings.Str3592);

			var enumerator = Strategies.GetEnumerator();

			while (enumerator.MoveNext())
			{
				var strategy = (StrategyContainer)enumerator.Current;

				strategy.CheckCanStart();

				var container = new StrategyContainer
				{
					Id = strategy.Id,
					StrategyInfo = _infoClone,
					MarketDataSettings = strategy.MarketDataSettings,
					Connector = EmulationConnector,
					//SessionType = SessionType.Optimization,
				};

				container.Environment.AddRange(strategy.Environment);

				container.SetCandleManager(CreateCandleManager());
				container.SetIsEmulation(true);
				container.SetIsInitialization(false);

				container.NameGenerator.Pattern = strategy.NameGenerator.Pattern;
				container.Portfolio = strategy.Portfolio;
				container.Security = strategy.Security;
				container.Strategy = strategy;

				container.UnrealizedPnLInterval = EmulationSettings.UnrealizedPnLInterval ?? ((EmulationSettings.StopTime - EmulationSettings.StartTime).Ticks / 1000).To<TimeSpan>();

				_infoClone.Strategies.Add(container);

				yield return container;
			}

			EmulationConnector.AddInfoLog(LocalizedStrings.Str3593);
		}

		private void BasketEmulationOnStateChanged(EmulationStates oldState, EmulationStates newState)
		{
			switch (_basketEmulation.State)
			{
				case EmulationStates.Stopped:
				{
					OnEmulationStopped();

					IsInProgress = false;
					Progress = 0;

					new ChartAutoRangeCommand(false).Process(Strategy.Strategy);

					CanStart = true;
					break;
				}

				case EmulationStates.Started:
				{
					if (oldState != EmulationStates.Starting)
						break;

					_prevEmulTime = DateTimeOffset.MinValue;
					_prevSystemTime = DateTime.Now;
					_emulDuration = TimeSpan.Zero;

					IsInProgress = true;

					CanStop = true;

					new ChartAutoRangeCommand(true).Process(Strategy.Strategy);

					break;
				}
			}
		}

		public void RefreshStatistics()
		{
			if (EmulationConnector.State != EmulationStates.Stopped)
			{
				var timeElapsed = DateTime.Now - _prevSystemTime;

				if (timeElapsed.TotalSeconds < 1)
					return;

				var emuElapsed = EmulationConnector.CurrentTime - _prevEmulTime;

				if (emuElapsed.TotalSeconds < 1)
				{
					_prevSystemTime = DateTime.Now;
					return;
				}

				MarketTime = EmulationConnector.CurrentTime;
				ErrorCount = EmulationConnector.ErrorCount;

				_emulDuration += timeElapsed;

				Duration = _emulDuration;
				Remaining = Progress == 0
					? TimeSpan.MaxValue
					: TimeSpan.FromTicks((_emulDuration.Ticks * 100) / Progress);

				_prevSystemTime = DateTime.Now;
				_prevEmulTime = EmulationConnector.CurrentTime;
			}
			//else
			//	ResetStatistics();
		}

		//private void ResetStatistics()
		//{
		//	MarketTime = DateTime.MinValue;
		//	ErrorCount = 0;
		//	Duration = TimeSpan.Zero;
		//	Remaining = TimeSpan.Zero;
		//	//EmulationSettings.MessagesLeft = 0;

		//	_prevSystemTime = DateTime.MinValue;
		//	_prevEmulTime = DateTime.MinValue;
		//}

		private void CreateEmulationSession()
		{
			EmulationConnector.AddInfoLog(LocalizedStrings.Str3594);

			var registry = ConfigManager.GetService<IStudioEntityRegistry>();

			_emulationSession = new Session
			{
				StartTime = DateTime.Now,
				Type = SessionType.Emulation
			};
			_emulationSession.Settings.SetValue("EmulationSettings", EmulationSettings);

			registry.Sessions.Add(_emulationSession);
			registry.Sessions.DelayAction.WaitFlush();

			EmulationConnector.AddInfoLog(LocalizedStrings.Str3595);

			var strategyInfoList = registry.GetStrategyInfoList(_emulationSession);

			_infoClone = Strategy.StrategyInfo.Clone();
			_infoClone.Id = 0;

			strategyInfoList.Add(_infoClone);
		}

		private CandleManager CreateCandleManager()
		{
			var candleManager = new CandleManager(EmulationConnector);
			candleManager.Sources.RemoveWhere(s => s is StorageCandleSource);
			return candleManager;
		}

		private void SaveSession(Action<Session> action)
		{
			if (_emulationSession == null)
				return;

			action(_emulationSession);

			ConfigManager.GetService<IStudioEntityRegistry>().Sessions.Save(_emulationSession);
		}

		private void ResetEmulationSession()
		{
			SaveSession(s => s.EndTime = DateTime.Now);

			_emulationSession = null;
		}

		private void OnEmulationStopped()
		{
			foreach (var strategy in _basketEmulation.BatchStrategies.OfType<StrategyContainer>())
			{
				strategy.Strategy = null;
			}

			ResetEmulationSession();

			NotifyPropertyChangedExHelper.Filter = null;
		}

		public void Load(SettingsStorage storage)
		{
			EmulationSettings.Load(storage.GetValue<SettingsStorage>("EmulationSettings"));
		}

		public void Save(SettingsStorage storage)
		{
			storage.SetValue("EmulationSettings", EmulationSettings.Save());
		}
	}
}