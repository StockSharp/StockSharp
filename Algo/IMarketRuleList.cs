namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;

	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс, описывающий список правил.
	/// </summary>
	public interface IMarketRuleList : ISynchronizedCollection<IMarketRule>
	{
		/// <summary>
		/// Получить все активные токены правил.
		/// </summary>
		IEnumerable<object> Tokens { get; }

		/// <summary>
		/// Получить все правила, ассоциированные с токеном.
		/// </summary>
		/// <param name="token">Токен правила.</param>
		/// <returns>Все правила, ассоциированные с токеном.</returns>
		IEnumerable<IMarketRule> GetRulesByToken(object token);

		/// <summary>
		/// Удалить все правила, у которых <see cref="IMarketRule.Token"/> равен <paramref name="token"/>.
		/// </summary>
		/// <param name="token">Токен правила.</param>
		/// <param name="currentRule">Текущее правило, которое инициировало удаление. Если оно было передано, то оно не будет удалено.</param>
		void RemoveRulesByToken(object token, IMarketRule currentRule);
	}

	/// <summary>
	/// Список правил.
	/// </summary>
	public class MarketRuleList : SynchronizedSet<IMarketRule>, IMarketRuleList
	{
		private readonly IMarketRuleContainer _container;
		private readonly Dictionary<object, HashSet<IMarketRule>> _rulesByToken = new Dictionary<object, HashSet<IMarketRule>>(); 

		/// <summary>
		/// Создать <see cref="MarketRuleList"/>.
		/// </summary>
		/// <param name="container">Контейнер правил.</param>
		public MarketRuleList(IMarketRuleContainer container)
		{
			if (container == null)
				throw new ArgumentNullException("container");

			_container = container;
		}

		/// <summary>
		/// Добавление элемента.
		/// </summary>
		/// <param name="item">Элемент.</param>
		protected override void OnAdded(IMarketRule item)
		{
			if (item.Token != null)
				_rulesByToken.SafeAdd(item.Token).Add(item);

			item.Container = _container;
			base.OnAdded(item);
		}

		/// <summary>
		/// Удаление элемента.
		/// </summary>
		/// <param name="item">Элемент.</param>
		/// <returns>Признак возможности действия.</returns>
		protected override bool OnRemoving(IMarketRule item)
		{
			if (!Contains(item))
				throw new InvalidOperationException(LocalizedStrings.Str906Params.Put(item.Name, _container.Name));

			return base.OnRemoving(item);
		}

		/// <summary>
		/// Удаление элемента.
		/// </summary>
		/// <param name="item">Элемент.</param>
		protected override void OnRemoved(IMarketRule item)
		{
			item.Container.AddRuleLog(LogLevels.Debug, item, LocalizedStrings.Str907);

			if (item.Token != null)
			{
				var set = _rulesByToken[item.Token];
				set.Remove(item);

				if (set.IsEmpty())
					_rulesByToken.Remove(item.Token);
			}

			item.Dispose();

			base.OnRemoved(item);
		}

		/// <summary>
		/// Очищение элементов.
		/// </summary>
		/// <returns>Признак возможности действия.</returns>
		protected override bool OnClearing()
		{
			foreach (var item in ToArray())
				Remove(item);

			return base.OnClearing();
		}

		IEnumerable<object> IMarketRuleList.Tokens
		{
			get
			{
				lock (SyncRoot)
					return _rulesByToken.Keys.ToArray();
			}
		}

		/// <summary>
		/// Получить все правила, ассоциированные с токеном.
		/// </summary>
		/// <param name="token">Токен правила.</param>
		/// <returns>Все правила, ассоциированные с токеном.</returns>
		public IEnumerable<IMarketRule> GetRulesByToken(object token)
		{
			lock (SyncRoot)
			{
				var set = _rulesByToken.TryGetValue(token);

				return set == null ? Enumerable.Empty<IMarketRule>() : set.ToArray();
			}
		}

		void IMarketRuleList.RemoveRulesByToken(object token, IMarketRule currentRule)
		{
			lock (SyncRoot)
			{
				foreach (var rule in GetRulesByToken(token))
				{
					if (currentRule == rule)
						continue;

					Remove(rule);
				}
			}
		}
	}
}