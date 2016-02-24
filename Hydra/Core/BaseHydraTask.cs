#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: BaseHydraTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Configuration;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Базовая класс для задачи.
	/// </summary>
	public abstract class BaseHydraTask : BaseLogReceiver, IHydraTask, INotifyPropertyChanged
	{
		private readonly SyncObject _syncObject = new SyncObject();
		private int _currentErrorCount;

		/// <summary>
		/// Событие запуска.
		/// </summary>
		public event Action<IHydraTask> Started;

		/// <summary>
		/// Событие остановки.
		/// </summary>
		public event Action<IHydraTask> Stopped;

		/// <summary>
		/// Сохранить инструмент если он отсутствует в <see cref="IEntityRegistry.Securities"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		protected void SaveSecurity(Security security)
		{
			if (EntityRegistry.Securities.ReadById(security.Id) != null)
				return;

			EntityRegistry.Securities.Save(security);
			this.AddInfoLog(LocalizedStrings.Str2188Params, security);
		}

		/// <summary>
		/// Название источника (для различия в лог файлах).
		/// </summary>
		public override string Name => Settings == null ? this.GetDisplayName() : Settings.Title;

		/// <summary>
		/// Уровень логирования для источника.
		/// </summary>
		public override LogLevels LogLevel => Settings?.LogLevel ?? base.LogLevel;

		/// <summary>
		/// Адрес иконки, для визуального обозначения.
		/// </summary>
		public Uri Icon => GetType().GetIcon();

		/// <summary>
		/// Хранилище торговых объектов.
		/// </summary>
		public HydraEntityRegistry EntityRegistry => ConfigManager.GetService<HydraEntityRegistry>();

		/// <summary>
		/// Хранилище маркет-данных.
		/// </summary>
		public IStorageRegistry StorageRegistry => ConfigManager.GetService<IStorageRegistry>();

		private HydraTaskSettings _settings;

		/// <summary>
		/// Настройки задачи <see cref="IHydraTask"/>.
		/// </summary>
		public virtual HydraTaskSettings Settings => _settings;

		/// <summary>
		/// Инициализировать задачу.
		/// </summary>
		/// <param name="settings">Настройки задачи.</param>
		public void Init(HydraTaskSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			if (settings.Title.IsEmpty())
				settings.Title = this.GetDisplayName();

			Id = settings.Id;

			_settings = settings;
			ApplySettings(_settings);

			//Settings.PropertyChanged -= SettingsPropertyChanged;
			Settings.PropertyChanged += SettingsPropertyChanged;
		}

		/// <summary>
		/// Сохранить настройки источника.
		/// </summary>
		public void SaveSettings()
		{
			EntityRegistry.TasksSettings.Save(Settings); // Settings переопределен на возврат классов специфических настроек
		}

		/// <summary>
		/// Применить настройки.
		/// </summary>
		/// <param name="settings">Настройки.</param>
		protected abstract void ApplySettings(HydraTaskSettings settings);

		private TaskStates _state;

		/// <summary>
		/// Текущее состояние задачи.
		/// </summary>
		public TaskStates State
		{
			get { return _state; }
			private set
			{
				if (_state == value)
					return;

				switch (value)
				{
					case TaskStates.Stopped:
						break;
					case TaskStates.Stopping:
						if (_state == TaskStates.Stopped)
							throw new InvalidOperationException(LocalizedStrings.Str2189Params.Put(_state, value));
						break;
					case TaskStates.Starting:
						if (_state != TaskStates.Stopped)
							throw new InvalidOperationException(LocalizedStrings.Str2189Params.Put(_state, value));
						break;
					case TaskStates.Started:
						if (_state != TaskStates.Starting)
							throw new InvalidOperationException(LocalizedStrings.Str2189Params.Put(_state, value));
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(value));
				}

				_state = value;
				this.AddInfoLog(LocalizedStrings.Str2190Params, value);
			}
		}

		/// <summary>
		/// Запустить.
		/// </summary>
		public void Start()
		{
			if (State != TaskStates.Stopped)
				return;

			_currentErrorCount = 0;

			ThreadingHelper.Thread(() =>
			{
				try
				{
					WaitIfNecessary(TimeSpan.Zero);

					State = TaskStates.Starting;
					OnStarting();

					var attempts = 60;

					while (State == TaskStates.Starting && attempts-- > 0)
						WaitIfNecessary(TimeSpan.FromSeconds(1));

					if (State == TaskStates.Starting)
						this.AddErrorLog(LocalizedStrings.Str2191);

					while (State == TaskStates.Started)
					{
						try
						{
							var interval = OnProcess();

							if (interval == TimeSpan.MaxValue)
							{
								Stop();
								break;
							}

							// сбрасываем счетчик при успешной итерации
							_currentErrorCount = 0;

							WaitIfNecessary(interval);
						}
						catch (Exception ex)
						{
							HandleError(ex);
							WaitIfNecessary(TimeSpan.FromSeconds(5));
						}
					}
				}
				catch (Exception ex)
				{
					this.AddErrorLog(ex);
					this.AddErrorLog(LocalizedStrings.Str2192);
				}
				finally
				{
					try
					{
						OnStopped();
					}
					catch (Exception ex)
					{
						this.AddErrorLog(ex);
					}

					Stopped.SafeInvoke(this);
					State = TaskStates.Stopped;
				}
			})
			.Name("{0} Task thread".Put(Name))
			.Launch();
		}

		/// <summary>
		/// Обработать ошибку.
		/// </summary>
		/// <param name="error">Ошибка.</param>
		protected void HandleError(Exception error)
		{
			this.AddErrorLog(error);

			if (Settings.MaxErrorCount == 0 || ++_currentErrorCount < Settings.MaxErrorCount)
				return;

			this.AddErrorLog(LocalizedStrings.Str2193);
			State = TaskStates.Stopping;
		}

		/// <summary>
		/// Остановить.
		/// </summary>
		public void Stop()
		{
			lock (_syncObject)
			{
				State = TaskStates.Stopping;
				_syncObject.Pulse();
			}
		}

		/// <summary>
		/// Действие при запуске загрузки данных.
		/// </summary>
		protected virtual void OnStarting()
		{
			RaiseStarted();
		}

		/// <summary>
		/// Вызвать событие <see cref="Started"/>.
		/// </summary>
		protected void RaiseStarted()
		{
			State = TaskStates.Started;
			Started.SafeInvoke(this);
		}

		/// <summary>
		/// Действие при остановке загрузки данных.
		/// </summary>
		protected virtual void OnStopped()
		{
		}

		/// <summary>
		/// Можно ли продолжить работу задачи в методе <see cref="OnProcess"/>.
		/// </summary>
		/// <param name="chechTime">Проверять рабочее время.</param>
		/// <returns><see langword="true"/>, если работу продолжить возможно, иначе, работу метода необходимо прервать.</returns>
		protected bool CanProcess(bool chechTime = true)
		{
			return State == TaskStates.Started && (!chechTime || CheckWorkingTime() == TimeSpan.Zero);
		}

		private TimeSpan CheckWorkingTime()
		{
			var now = DateTime.Now;
			var from = now.Date + Settings.WorkingFrom;
			var to = now.Date + Settings.WorkingTo;

			// например, с 10:00 текущего дня до 01:00 следующего дня
			if (Settings.WorkingFrom >= Settings.WorkingTo)
				to += TimeSpan.FromDays(1);

			if (now < from || now > to)
			{
				this.AddInfoLog(LocalizedStrings.Str1126Params, now.ToString("T"), Settings.WorkingFrom, Settings.WorkingTo);

				var nextStart = now < from ? from : from.AddDays(1);
				var interval = nextStart - now;

				this.AddInfoLog(LocalizedStrings.Str2197Params, nextStart.ToString("G"));
				return interval;
			}

			return TimeSpan.Zero;
		}

		private void WaitIfNecessary(TimeSpan interval)
		{
			lock (_syncObject)
			{
				if (State != TaskStates.Starting && State != TaskStates.Started)
					return;

				interval = interval.Max(CheckWorkingTime());
				_syncObject.Wait(interval);
			}
		}

		/// <summary>
		/// Выполнить задачу.
		/// </summary>
		/// <returns>Минимальный интервал, после окончания которого необходимо снова выполнить задачу.</returns>
		protected virtual TimeSpan OnProcess()
		{
			return Settings.Interval;
		}

		/// <summary>
		/// Поддерживаемые типы данных.
		/// </summary>
		public abstract IEnumerable<DataType> SupportedDataTypes { get; }

		private void SafeSave<T>(Security security, object arg, IEnumerable<T> values, Func<T, DateTimeOffset> getTime, IEnumerable<Func<T, string>> getErrors)
			where T : Message
		{
			SafeSave(security, typeof(T), arg, values, getTime, getErrors, (s, d, f) => (IMarketDataStorage<T>)StorageRegistry.GetStorage(s, typeof(T), arg, d, f));
		}

		private void SafeSave<T>(Security security, Type dataType, object arg, IEnumerable<T> values, Func<T, DateTimeOffset> getTime, IEnumerable<Func<T, string>> getErrors, Func<Security, IMarketDataDrive, StorageFormats, IMarketDataStorage<T>> getStorage)
			where T : Message
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (Settings.MaxErrorCount == 0)
			{
				var valuesWithResult = values.Select(v =>
				{
					foreach (var check in getErrors)
					{
						var msg = check(v);

						if (!msg.IsEmpty())
							return Tuple.Create(v, msg);
					}

					return Tuple.Create(v, string.Empty);
				});

				var dict = valuesWithResult
					.GroupBy(t => t.Item2)
					.ToDictionary(
						g => g.Key,
						g => g.Select(t => t.Item1).ToArray()
					);

				foreach (var pair in dict)
				{
					if (!pair.Key.IsEmpty())
					{
						this.AddWarningLog(LocalizedStrings.Str2198Params,
							security.Id, pair.Value.Length, dataType.Name, pair.Key);
					}
				}

				values = (dict.TryGetValue(string.Empty) ?? Enumerable.Empty<T>()).ToArray();
			}

			var count = values.Count();

			if (count == 0)
				return;

			try
			{
				getStorage(security, Settings.Drive, Settings.StorageFormat).Save(values);
				RaiseDataLoaded(security, dataType, arg, getTime(values.Last()), count);
			}
			catch (Exception ex)
			{
				this.AddErrorLog(ex);

				if (Settings.MaxErrorCount > 0)
					throw;
			}
		}

		private static Func<T, string> CreateErrorCheck<T>(Func<T, bool> check, string message)
		{
			return v => check(v) ? message : string.Empty;
		}

		/// <summary>
		/// Сохранить тиковые сделки в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="ticks">Тиковые сделки.</param>
		protected void SaveTicks(HydraTaskSecurity security, IEnumerable<ExecutionMessage> ticks)
		{
			SaveTicks(security.Security, ticks);
		}

		/// <summary>
		/// Сохранить тиковые сделки в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="ticks">Тиковые сделки.</param>
		protected void SaveTicks(Security security, IEnumerable<ExecutionMessage> ticks)
		{
			SafeSave(security, ExecutionTypes.Tick, ticks, t => t.ServerTime, new[]
			{
				// execution ticks (like option execution) may be a zero cost
				// ticks for spreads may be a zero cost or less than zero
				//CreateErrorCheck<Trade>(t => t.Price <= 0, LocalizedStrings.Str2199),

				CreateErrorCheck<ExecutionMessage>(t => security.PriceStep != null && t.TradePrice % security.PriceStep != 0, LocalizedStrings.Str2200)
			});
		}

		/// <summary>
		/// Сохранить стаканы в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="depths">Стаканы.</param>
		protected void SaveDepths(HydraTaskSecurity security, IEnumerable<QuoteChangeMessage> depths)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			SaveDepths(security.Security, depths);
		}

		/// <summary>
		/// Сохранить стаканы в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="depths">Стаканы.</param>
		protected void SaveDepths(Security security, IEnumerable<QuoteChangeMessage> depths)
		{
			SafeSave(security, null, depths, d => d.ServerTime,
				Enumerable.Empty<Func<QuoteChangeMessage, string>>()
				// TODO
				//new[]
				//{
				//	CreateErrorCheck<QuoteChangeMessage>(d => (d.BestPair != null && d.BestPair.IsFull && d.BestBid.Price > d.BestAsk.Price), LocalizedStrings.Str2201)
				
				//	// quotes for spreads may be a zero cost or less than zero
				//	//CreateErrorCheck<MarketDepth>(d => d.Any(q => q.Price <= 0), LocalizedStrings.Str2202)
				//}
			);
		}

		/// <summary>
		/// Сохранить лог заявок по инструменту в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="items">Лог заявок.</param>
		protected void SaveOrderLog(HydraTaskSecurity security, IEnumerable<ExecutionMessage> items)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			SaveOrderLog(security.Security, items);
		}

		/// <summary>
		/// Сохранить лог заявок по инструменту в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="items">Лог заявок.</param>
		protected void SaveOrderLog(Security security, IEnumerable<ExecutionMessage> items)
		{
			SafeSave(security, ExecutionTypes.OrderLog, items,
				i => i.ServerTime, Enumerable.Empty<Func<ExecutionMessage, string>>());
		}

		/// <summary>
		/// Сохранить изменения по инструменту в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="messages">Изменения.</param>
		protected void SaveLevel1Changes(HydraTaskSecurity security, IEnumerable<Level1ChangeMessage> messages)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			SaveLevel1Changes(security.Security, messages);
		}

		/// <summary>
		/// Сохранить изменения по инструменту в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="messages">Изменения.</param>
		protected void SaveLevel1Changes(Security security, IEnumerable<Level1ChangeMessage> messages)
		{
			SafeSave(security, null, messages, c => c.ServerTime, new[]
			{
				CreateErrorCheck<Level1ChangeMessage>(m => m.Changes.IsEmpty(), LocalizedStrings.Str920)
			});
		}

		/// <summary>
		/// Сохранить свечи по инструменту в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="candles">Свечи.</param>
		protected void SaveCandles(HydraTaskSecurity security, IEnumerable<CandleMessage> candles)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			SaveCandles(security.Security, candles);
		}

		/// <summary>
		/// Сохранить свечи по инструменту в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="candles">Свечи.</param>
		protected void SaveCandles(Security security, IEnumerable<CandleMessage> candles)
		{
			candles
				.GroupBy(c => Tuple.Create(c.GetType(), c.Arg))
				.ForEach(g => SafeSave(security, g.Key.Item1, g.Key.Item2, g, c => c.OpenTime, new[]
				{
					CreateErrorCheck<CandleMessage>(c => security.PriceStep != null && security.PriceStep != 0 && c.OpenPrice % security.PriceStep != 0, LocalizedStrings.Str2203),
					CreateErrorCheck<CandleMessage>(c => security.PriceStep != null && security.PriceStep != 0 && c.HighPrice % security.PriceStep != 0, LocalizedStrings.Str2204),
					CreateErrorCheck<CandleMessage>(c => security.PriceStep != null && security.PriceStep != 0 && c.LowPrice % security.PriceStep != 0, LocalizedStrings.Str2205),
					CreateErrorCheck<CandleMessage>(c => security.PriceStep != null && security.PriceStep != 0 && c.ClosePrice % security.PriceStep != 0, LocalizedStrings.Str2206)
				},
				(s, d, c) => StorageRegistry.GetCandleMessageStorage(g.Key.Item1, security, g.Key.Item2, d, c)));
		}

		/// <summary>
		/// Сохранить новости в хранилище.
		/// </summary>
		/// <param name="news">Новости.</param>
		protected void SaveNews(IEnumerable<NewsMessage> news)
		{
			news = news.ToArray();

			if (!news.Any())
				return;

			var storage = StorageRegistry.GetNewsMessageStorage(Settings.Drive, Settings.StorageFormat);
			storage.Save(news);

			RaiseDataLoaded(null, typeof(NewsMessage), null, news.Last().ServerTime, news.Count());
		}

		/// <summary>
		/// Сохранить транзакции в хранилище.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="transactions">Транзакции.</param>
		protected void SaveTransactions(Security security, IEnumerable<ExecutionMessage> transactions)
		{
			foreach (var group in transactions.GroupBy(e => e.ExecutionType))
			{
				SafeSave(security, group.Key, group, t => t.ServerTime, Enumerable.Empty<Func<ExecutionMessage, string>>());
			}
		}

		/// <summary>
		/// Вызывать событие <see cref="DataLoaded"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="dataType">Тип данных.</param>
		/// <param name="arg">Параметр данных.</param>
		/// <param name="time">Время последних данных.</param>
		/// <param name="count">Количество последних данных.</param>
		protected void RaiseDataLoaded(Security security, Type dataType, object arg, DateTimeOffset time, int count)
		{
			if (security == null)
				this.AddInfoLog(LocalizedStrings.Str2207Params, count, dataType.Name);
			else
				this.AddInfoLog(LocalizedStrings.Str2208Params, security.Id, count, dataType.Name, arg);

			DataLoaded.SafeInvoke(security, dataType, arg, time, count);
		}

		/// <summary>
		/// Событие заргрузки данных. 
		/// </summary>
		public event Action<Security, Type, object, DateTimeOffset, int> DataLoaded;

		private void NotifyPropertyChanged(string name)
		{
			PropertyChanged.SafeInvoke(this, name);
		}

		/// <summary>
		/// Получить список инструментов, с которыми будет работать данный источник.
		/// </summary>
		/// <returns>Инструменты.</returns>
		protected IEnumerable<HydraTaskSecurity> GetWorkingSecurities()
		{
			return this.GetAllSecurity() == null
					? Settings.Securities
					: this.ToHydraSecurities(EntityRegistry.Securities.Where(s => !s.IsAllSecurity()));
		}

		/// <summary>
		/// Получить инструмент по идентификатору.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Инструмент.</returns>
		protected Security GetSecurity(SecurityId securityId)
		{
			var security = EntityRegistry.Securities.ReadBySecurityId(securityId);

			if (security == null)
			{
				security = new Security
				{
					Id = securityId.ToStringId(),
				};

				SaveSecurity(security);
			}

			return security;
		}

		/// <summary>
		/// Получить путь для временной директории.
		/// </summary>
		/// <returns>Временная директория.</returns>
		protected string GetTempPath()
		{
			var locDrive = Settings.Drive as LocalMarketDataDrive;
			var tempPath = Path.Combine((locDrive ?? DriveCache.Instance.DefaultDrive).Path, "TemporaryFiles");

			if (!Directory.Exists(tempPath))
				Directory.CreateDirectory(tempPath);

			return tempPath;
		}

		/// <summary>
		/// Событие изменения настроек.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		private void SettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			NotifyPropertyChanged("Settings." + e.PropertyName);
		}
	}
}