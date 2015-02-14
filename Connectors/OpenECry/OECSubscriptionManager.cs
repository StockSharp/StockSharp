using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Logging;
using StockSharp.BusinessEntities;
using A = OEC.API;
using D = OEC.Data;

namespace StockSharp.OEC
{
	/// <summary>
	/// Управление подписками на обновления по портфелям, котировкам, стаканам, тикам.
	/// </summary>
	class OECSubscriptionManager
	{
		private readonly OECTrader _trader;
		private bool _subscribed;

		private A.OECClient Oec
		{
			get { return _trader.Oec; }
		}

		private readonly Dictionary<string, int> _accounts = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _prices = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _doms = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _ticks = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _ticksSubscriptionIds = new Dictionary<string, int>();

		public OECSubscriptionManager(OECTrader trader)
		{
			_trader = trader;
		}

		public void ResubscribeAll()
		{
			if (Oec == null)
			{
				_trader.AddErrorLog("ResubscribeAll: OEC не инициализирован (Oec == null).");
				return;
			}

			UnsubscribeAll();

			try
			{
				foreach (var sub in _prices)
					for (var i = 0; i < sub.Value; ++i)
						Oec.Subscribe(Oec.Contracts[sub.Key]);

				foreach (var sub in _doms)
					for (var i = 0; i < sub.Value; ++i)
						Oec.SubscribeDOM(Oec.Contracts[sub.Key]);

				foreach (var sub in _ticks)
				{
					EnsureDictionary(_ticksSubscriptionIds, sub.Key);
					_ticksSubscriptionIds[sub.Key] = Oec.SubscribeTicks(Oec.Contracts[sub.Key], 0).ID;
				}
			}
			catch (Exception e)
			{
				_trader.AddErrorLog("ResubscribeAll: ошибка во время переподписки: {0}", e);
			}

			_subscribed = true;
		}

		public void UnsubscribeAll()
		{
			try
			{
				foreach (var sub in _prices)
					for (var i = 0; i < sub.Value; ++i)
						Oec.Unsubscribe(Oec.Contracts[sub.Key]);

				foreach (var sub in _doms)
					for (var i = 0; i < sub.Value; ++i)
						Oec.UnsubscribeDOM(Oec.Contracts[sub.Key]);

				foreach (var sub in _ticks)
				{
					EnsureDictionary(_ticksSubscriptionIds, sub.Key);

					var subId = _ticksSubscriptionIds[sub.Key];
					if (subId <= 0 || Oec.Subscriptions[subId] == null)
						continue;

					Oec.CancelSubscription(Oec.Subscriptions[subId]);
					_ticksSubscriptionIds[sub.Key] = 0;
				}
			}
			catch (Exception e)
			{
				_trader.AddErrorLog("UnsubscribeAll: ошибка во время отмены подписки: {0}", e);
			}

			_subscribed = false;
		}

		public void ClearAll()
		{
			UnsubscribeAll();
			_accounts.Clear();
			_prices.Clear();
			_doms.Clear();
			_ticks.Clear();
			_ticksSubscriptionIds.Clear();
		}

		//--------------------------------------------------

		public void SubscribePortfolio(Portfolio portfolio)
		{
			EnsureDictionary(_accounts, portfolio.Name);
			++_accounts[portfolio.Name];
		}

		public void UnsubscribePortfolio(Portfolio portfolio)
		{
			EnsureDictionary(_accounts, portfolio.Name);
			if (_accounts[portfolio.Name] > 0)
				--_accounts[portfolio.Name];
		}

		public bool DiscardPortfolioData(string portfolioName)
		{
			EnsureDictionary(_accounts, portfolioName);
			return !(_accounts[portfolioName] > 0);
		}

		//--------------------------------------------------

		public void SubscribeSecurity(Security sec)
		{
			var name = VerifyContract(sec, _prices);
			if (++_prices[name] != 1 || !_subscribed)
				return;

			Oec.Subscribe(Oec.Contracts[name]);
		}

		public void UnsubscribeSecurity(Security sec)
		{
			var name = VerifyContract(sec, _prices);
			if (!(_prices[name] > 0))
				return;
			if (--_prices[name] > 0 || !_subscribed)
				return;

			Oec.Unsubscribe(Oec.Contracts[name]);
		}

		public bool DiscardSecurityData(A.Contract contract)
		{
			return !(_prices[VerifyContract(contract, _prices)] > 0);
		}

		//--------------------------------------------------

		public void SubscribeQuotes(Security sec)
		{
			var name = VerifyContract(sec, _doms);
			if (++_doms[name] != 1 || !_subscribed)
				return;

			Oec.SubscribeDOM(Oec.Contracts[name]);
		}

		public void UnsubscribeQuotes(Security sec)
		{
			var name = VerifyContract(sec, _doms);
			if (!(_doms[name] > 0))
				return;
			if (--_doms[name] > 0 || !_subscribed)
				return;

			Oec.UnsubscribeDOM(Oec.Contracts[name]);
		}

		public bool DiscardQuotesData(A.Contract contract)
		{
			//return !(_doms[VerifyContract(sec, _doms)] > 0);
			return false; // DOM require explicit subscription for each contract, so if data comes - it means there is subscription.
		}

		//--------------------------------------------------

		public void SubscribeTrades(Security sec)
		{
			var name = VerifyContract(sec, _ticks);

			if (++_ticks[name] != 1)
				return;

			EnsureDictionary(_ticksSubscriptionIds, name);

			_ticksSubscriptionIds[name] = 0;
			if (_subscribed)
				_ticksSubscriptionIds[name] = Oec.SubscribeTicks(Oec.Contracts[name], 0).ID;
		}

		public void UnsubscribeTrades(Security sec)
		{
			var name = VerifyContract(sec, _ticks);

			EnsureDictionary(_ticksSubscriptionIds, name);

			if (!(_ticks[name] > 0))
				return;

			var sub = Oec.Subscriptions[_ticksSubscriptionIds[name]];

			if (--_ticks[name] == 0 && sub != null)
			{
				if (_subscribed)
					Oec.CancelSubscription(sub);
				_ticksSubscriptionIds[name] = 0;
			}
		}

		public bool DiscardTradesData(A.Contract contract)
		{
			//return _ticks[VerifyContract(sec, _ticks)] > 0;
			return false; // Ticks require explicit subscription for each contract, so if data comes - it means there is subscription.
		}

		//--------------------------------------------------

		private string VerifyContract<T>(Security sec, Dictionary<string, T> dic)
		{
			var contract = Oec.Contracts[sec.Code];

			if (contract == null)
				throw new OECTraderException("Контракт '{0}' не найден!".Put(sec.Code));

			EnsureDictionary(dic, sec.Code);
			return sec.Code;
		}

		private string VerifyContract<T>(A.Contract contract, Dictionary<string, T> dic)
		{
			EnsureDictionary(dic, contract.Symbol);
			return contract.Symbol;
		}

		private void EnsureDictionary<TKType, TVType>(Dictionary<TKType, TVType> dic, TKType key)
		{
			if (!dic.ContainsKey(key))
				dic[key] = default(TVType);
		}
	}
}