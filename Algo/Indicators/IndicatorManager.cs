namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// Менеджер индикаторов, который автоматически заполняет индикаторы <see cref="Indicators"/> на основе поступаемых данных.
	/// </summary>
	public class IndicatorManager : Disposable
	{
		private sealed class IndicatorList : SynchronizedList<IndicatorToken>
		{
			private sealed class ResetEventHandler
			{
				public readonly IndicatorToken Token;
				private readonly IndicatorManager _manager;
				private Action ResetHandler { get; set; }

				public ResetEventHandler(IndicatorToken token, IndicatorManager manager)
				{
					Token = token;
					_manager = manager;

					ResetHandler = OnReset;
					Token.Indicator.Reseted += ResetHandler;
				}

				public void Unsubscribe()
				{
					Token.Indicator.Reseted -= ResetHandler;
				}

				private void OnReset()
				{
					_manager.Container.ClearValues(Token);
				}
			}

			private readonly IndicatorManager _manager;
			private readonly SynchronizedList<ResetEventHandler> _resetHandlers = new SynchronizedList<ResetEventHandler>();

			public IIndicatorSource Source { get; private set; }

			public IndicatorList(IndicatorManager manager, IIndicatorSource source)
			{
				if (manager == null)
					throw new ArgumentNullException("manager");

				if (source == null)
					throw new ArgumentNullException("source");

				_manager = manager;
				Source = source;
				Source.NewValue += OnNewValue;
			}

			protected override void OnAdded(IndicatorToken item)
			{
				if (item == null)
					throw new ArgumentNullException("item");

				_resetHandlers.Add(new ResetEventHandler(item, _manager));
				item.Indicator.Reset();

				_manager.ProcessIndicator(item);

				base.OnAdded(item);
			}

			protected override bool OnRemoving(IndicatorToken item)
			{
				var handler = _resetHandlers.First(h => h.Token == item);
				handler.Unsubscribe();
				_resetHandlers.Remove(handler);
				_manager.Container.ClearValues(item);

				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				foreach (var handler in _resetHandlers)
					handler.Unsubscribe();

				_resetHandlers.Clear();
				return base.OnClearing();
			}

			private void OnNewValue(IIndicatorValue value)
			{
				_manager.Container.AddInput(Source, value);

				lock (SyncRoot)
				{
					foreach (var ind in this)
					{
						_manager.ProcessIndicator(ind, value);
					}
				}

				_manager.RaiseNewValueProcessed(Source, value);
			}

			public void Unsubscribe()
			{
				Source.NewValue -= OnNewValue;
			}
		}

		private readonly CachedSynchronizedDictionary<IndicatorToken, RefPair<IndicatorToken, int>> _indicatorUsages =
			new CachedSynchronizedDictionary<IndicatorToken, RefPair<IndicatorToken, int>>();

		private readonly CachedSynchronizedDictionary<IIndicatorSource, IndicatorList> _sources =
			new CachedSynchronizedDictionary<IIndicatorSource, IndicatorList>();

		/// <summary>
		/// Создать <see cref="IndicatorManager"/>.
		/// </summary>
		public IndicatorManager()
			: this(new IndicatorContainer())
		{
		}

		/// <summary>
		/// Создать <see cref="IndicatorManager"/>.
		/// </summary>
		/// <param name="container">Контейнер, хранящий данные индикаторов.</param>
		public IndicatorManager(IIndicatorContainer container)
		{
			if (container == null)
				throw new ArgumentNullException("container");

			Container = container;
		}

		/// <summary>
		/// Контейнер, хранящий данные индикаторов.
		/// </summary>
		public IIndicatorContainer Container { get; private set; }

		/// <summary>
		/// Все токены индикаторов.
		/// </summary>
		public IEnumerable<IndicatorToken> IndicatorTokens
		{
			get { return _indicatorUsages.CachedKeys; }
		}

		/// <summary>
		/// Все зарегистрированные источники.
		/// </summary>
		public IEnumerable<IIndicatorSource> Sources
		{
			get { return _sources.CachedKeys; }
		}

		/// <summary>
		/// Событие обработки нового значения группой индикаторов.
		/// </summary>
		public event Action<IIndicatorSource, IIndicatorValue> NewValueProcessed;

		/// <summary>
		/// Зарегистрировать индикатор. После регистрации данный индикатор начнет обновляться с использованием переданного источника.
		/// Если по данному источнику уже есть сохраненные данные, то они будут использованы для инициализации индикатора.
		/// Если пара индикатор-источник уже была ранее зарегистрирована, то вернется существующий токен.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="source">Источник данных для индикатора.</param>
		/// <returns>Токен, который был зарегистрирован.</returns>
		public virtual IndicatorToken RegisterIndicator(IIndicator indicator, IIndicatorSource source)
		{
			lock (_indicatorUsages.SyncRoot)
			{
				var token = new IndicatorToken(indicator, source);

				var inDict = _indicatorUsages.TryGetValue(token);
				if (inDict != null)
				{
					//найден индикатор, увеличим количество ссылок
					inDict.Second++;
					return inDict.First;
				}

				//индикатора нет, добавим
				lock (_sources.SyncRoot)
				{
					var indicators = _sources.SafeAdd(source, key => new IndicatorList(this, key));

					token = new IndicatorToken(indicator, indicators.Source) { Container = Container };
					_indicatorUsages.Add(token, new RefPair<IndicatorToken, int>(token, 1));
					indicators.Add(token); // тут пройдет подписка на события источника и обработка тех значений, которые уже есть в источнике
				}

				return token;
			}
		}

		/// <summary>
		/// Сообщить, что данный токен больше не требуется.
		/// Если количество вызовов будет равно количеству вызовов <see cref="RegisterIndicator"/>, то все данные по индикатору будут удалены.
		/// </summary>
		/// <param name="indicatorToken">Токен индикатора.</param>
		public virtual void UnRegisterIndicator(IndicatorToken indicatorToken)
		{
			lock (_indicatorUsages.SyncRoot)
			{
				var inDict = _indicatorUsages[indicatorToken];

				if (inDict.Second <= 0)
					throw new InvalidOperationException();

				if (--inDict.Second == 0)
				{
					_indicatorUsages.Remove(indicatorToken);

					//чистим значения в контейнере
					Container.RemoveIndicator(indicatorToken);

					//удаляем из списка источника
					lock (_sources.SyncRoot)
					{
						var indicators = _sources.TryGetValue(indicatorToken.Source);
						if (indicators != null)
						{
							indicators.Remove(indicatorToken);
						}
					}
				}
			}
		}

		private void ProcessIndicator(IndicatorToken token)
		{
			lock (_indicatorUsages.SyncRoot)
			{
				var inputs = Container.GetInputs(token.Source);

				if (inputs != null)
					inputs.ForEach(i => ProcessIndicator(token, i));
			}
		}

		private void ProcessIndicator(IndicatorToken token, IIndicatorValue input)
		{
			if (token.Indicator.CanProcess(input))
			{
				Container.AddValue(token, input, token.Indicator.Process(input));
			}
		}

		private void RaiseNewValueProcessed(IIndicatorSource source, IIndicatorValue value)
		{
			NewValueProcessed.SafeInvoke(source, value);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			foreach (var source in _sources.Values)
			{
				source.Unsubscribe();
			}

			_sources.Clear();
			_indicatorUsages.Clear();

			base.DisposeManaged();
		}
	}
}