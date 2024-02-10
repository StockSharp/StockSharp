namespace StockSharp.Algo.Strategies.Derivatives
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Derivatives;
	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The base strategy of hedging.
	/// </summary>
	public abstract class HedgeStrategy : Strategy
	{
		private sealed class AssetStrategy : Strategy
		{
			public AssetStrategy(Security asset)
			{
				if (asset == null)
					throw new ArgumentNullException(nameof(asset));

				Name = asset.Id;
			}
		}

		private readonly SynchronizedDictionary<Security, Strategy> _strategies = new();
		//private bool _isSuspended;
		//private int _reHedgeOrders;
		private Strategy _assetStrategy;
		private readonly HashSet<Order> _awaitingOrders = new();
		private readonly SyncObject _syncRoot = new();

		/// <summary>
		/// Initialize <see cref="HedgeStrategy"/>.
		/// </summary>
		/// <param name="blackScholes"><see cref="BasketBlackScholes"/>.</param>
		protected HedgeStrategy(BasketBlackScholes blackScholes)
		{
			BlackScholes = blackScholes ?? throw new ArgumentNullException(nameof(blackScholes));

			_useQuoting = this.Param<bool>(nameof(UseQuoting));
			_priceOffset = this.Param<Unit>(nameof(PriceOffset), new());
		}

		/// <summary>
		/// Portfolio model for calculating the values of Greeks by the Black-Scholes formula.
		/// </summary>
		protected BasketBlackScholes BlackScholes { get; }

		private readonly StrategyParam<bool> _useQuoting;

		/// <summary>
		/// Whether to quote the registered order by the market price. The default mode is disabled.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.QuotingKey,
			Description = LocalizedStrings.UseQuotingDescKey,
			GroupName = LocalizedStrings.HedgingKey,
			Order = 0)]
		public bool UseQuoting
		{
			get => _useQuoting.Value;
			set => _useQuoting.Value = value;
		}

		private readonly StrategyParam<Unit> _priceOffset;

		/// <summary>
		/// The price shift for the registering order. It determines the amount of shift from the best quote (for the buy it is added to the price, for the sell it is subtracted).
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PriceOffsetKey,
			Description = LocalizedStrings.PriceOffsetForOrderKey,
			GroupName = LocalizedStrings.HedgingKey,
			Order = 1)]
		public Unit PriceOffset
		{
			get => _priceOffset.Value;
			set => _priceOffset.Value = value;
		}

		/// <summary>
		/// To get a list of rules on which the rehedging will respond.
		/// </summary>
		/// <returns>Rule list.</returns>
		protected virtual IEnumerable<IMarketRule> GetNotificationRules()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			yield return this.GetSecurity().WhenNewTrade(this);
#pragma warning restore CS0618 // Type or member is obsolete
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			//_reHedgeOrders = 0;
			_awaitingOrders.Clear();

			_strategies.Clear();

			var security = this.GetSecurity();

			if (_assetStrategy == null)
			{
				_assetStrategy = ChildStrategies.FirstOrDefault(s => s.Security == Security);
				
				if (_assetStrategy == null)
				{
					_assetStrategy = new AssetStrategy(security);
					ChildStrategies.Add(_assetStrategy);

					this.AddInfoLog(LocalizedStrings.AssetStrategyCreated);
				}
				else
					this.AddInfoLog(LocalizedStrings.AssetStrategyFound.Put(_assetStrategy));
			}

			_strategies.Add(security, _assetStrategy);

			if (BlackScholes.UnderlyingAsset == null)
			{
				BlackScholes.UnderlyingAsset = _assetStrategy.Security;
				this.AddInfoLog(LocalizedStrings.AssetPosSpecified);
			}

			BlackScholes.InnerModels.Clear();

			foreach (var strategy in ChildStrategies)
			{
				var childSec = strategy.GetSecurity();

				if (childSec.Type == SecurityTypes.Option && childSec.GetAsset(this) == security)
				{
					BlackScholes.InnerModels.Add(new BlackScholes(childSec, this, this, BlackScholes.ExchangeInfoProvider));
					_strategies.Add(childSec, strategy);

					this.AddInfoLog(LocalizedStrings.StrikeStrategyFound.Put(strategy));
				}
			}

			this.SuspendRules(() =>
				GetNotificationRules().Or()
					.Do(() => ReHedge(CurrentTime))
					.Apply(this));

			if (!IsRulesSuspended)
			{
				lock (_syncRoot)
					ReHedge(time);
			}
		}

		/// <summary>
		/// To get a list of orders rehedging the option position.
		/// </summary>
		/// <param name="currentTime">Current time.</param>
		/// <returns>Rehedging orders.</returns>
		protected abstract IEnumerable<Order> GetReHedgeOrders(DateTimeOffset currentTime);

		/// <summary>
		/// To add the rehedging strategy.
		/// </summary>
		/// <param name="parentStrategy">The parent strategy (by the strike or the underlying asset).</param>
		/// <param name="order">The rehedging order.</param>
		protected virtual void AddReHedgeQuoting(Strategy parentStrategy, Order order)
		{
			if (parentStrategy == null)
				throw new ArgumentNullException(nameof(parentStrategy));

			var quoting = CreateQuoting(order);

			quoting.Name = parentStrategy.Name + "_" + quoting.Name;

			quoting
				.WhenStopped()
				.Do((rule, s) => TryResumeMonitoring(order))
				.Once()
				.Apply(parentStrategy);

			parentStrategy.ChildStrategies.Add(quoting);
		}

		/// <summary>
		/// To add the rehedging order.
		/// </summary>
		/// <param name="parentStrategy">The parent strategy (by the strike or the underlying asset).</param>
		/// <param name="order">The rehedging order.</param>
		protected virtual void AddReHedgeOrder(Strategy parentStrategy, Order order)
		{
			var doneRule = order.WhenMatched(this)
				.Or(order.WhenCanceled(this))
				.Do((rule, o) =>
				{
					parentStrategy.AddInfoLog("Order {0} {1} in {2}.", o.TransactionId, o.IsMatched() ? LocalizedStrings.Done : LocalizedStrings.Cancelled, o.ServerTime);

					Rules.RemoveRulesByToken(o, rule);

					TryResumeMonitoring(order);
				})
				.Once()
				.Apply(parentStrategy);

			var regRule = order
				.WhenRegistered(this)
				.Do(o => parentStrategy.AddInfoLog("Order {0} registered with ID {1} in {2}.", o.TransactionId, o.Id, o.Time))
				.Once()
				.Apply(parentStrategy);

			var regFailRule = order
				.WhenRegisterFailed(this)
				.Do((rule, fail) =>
				{
					parentStrategy.AddErrorLog(LocalizedStrings.ErrorRegOrder, fail.Order.TransactionId, fail.Error);

					TryResumeMonitoring(order);
					ReHedge(fail.ServerTime);
				})
				.Once()
				.Apply(parentStrategy);

			doneRule.Exclusive(regFailRule);
			regRule.Exclusive(regFailRule);

			parentStrategy.RegisterOrder(order);
		}

		/// <summary>
		/// To start rehedging.
		/// </summary>
		/// <param name="orders">Rehedging orders.</param>
		protected virtual void ReHedge(IEnumerable<Order> orders)
		{
			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			foreach (var order in orders)
			{
				this.AddInfoLog("Rehedging with order {0} {1} volume {2} with price {3}.", order.Security, order.Side, order.Volume, order.Price);

				var strategy = _strategies.TryGetValue(order.Security);

				if (strategy == null)
					throw new InvalidOperationException(LocalizedStrings.ForSecurityNoChildStrategy.Put(order.Security.Id));

				if (UseQuoting)
				{
					AddReHedgeQuoting(strategy, order);
				}
				else
				{
					AddReHedgeOrder(strategy, order);
				}
			}
		}

		/// <summary>
		/// Whether the rehedging is paused.
		/// </summary>
		/// <returns><see langword="true" /> if paused, otherwise, <see langword="false" />.</returns>
		protected virtual bool IsSuspended()
		{
			return !_awaitingOrders.IsEmpty();
		}

		private void ReHedge(DateTimeOffset currentTime)
		{
			if (IsSuspended())
			{
				//this.AddWarningLog("Рехеджирование уже запущено.");
				return;
			}

			//_isSuspended = false;
			_awaitingOrders.Clear();

			var orders = GetReHedgeOrders(currentTime);

			_awaitingOrders.AddRange(orders);

			if (!_awaitingOrders.IsEmpty())
			{
				this.AddInfoLog(LocalizedStrings.ResumeSuspended, _awaitingOrders.Count);
				ReHedge(orders);
			}
		}

		private void TryResumeMonitoring(Order order)
		{
			if (!_awaitingOrders.Remove(order))
				return;

			if (_awaitingOrders.IsEmpty())
				this.AddInfoLog(LocalizedStrings.Resumed);
			else
				this.AddInfoLog(LocalizedStrings.PartRulesResumes, _awaitingOrders.Count);
		}

		/// <summary>
		/// To create a quoting strategy to change the position.
		/// </summary>
		/// <param name="order">Quoting order.</param>
		/// <returns>The strategy of quoting.</returns>
		protected virtual QuotingStrategy CreateQuoting(Order order)
		{
			return new MarketQuotingStrategy(order, new Unit(), new Unit()) { Volume = Volume };
		}
	}
}