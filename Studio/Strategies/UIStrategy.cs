namespace StockSharp.Studio.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3285Key)]
	[DescriptionLoc(LocalizedStrings.Str3286Key)]
	public class UIStrategy : Strategy//, IUIProvider
	{
		[DisplayNameLoc(LocalizedStrings.Str3287Key)]
		[DescriptionLoc(LocalizedStrings.Str3288Key)]
		private sealed class SharedPortfolio : WeighedVirtualPortfolio
		{
			public override IEnumerable<Order> SplitOrder(Order order)
			{
				var totalWeight = InnerPortfolios.Sum(p => p.Value);

				var orders = InnerPortfolios
					.Select(p =>
					{
						var newOrder = order.ReRegisterClone(newVolume: order.Balance);

						newOrder.Portfolio = p.Key;
						newOrder.Volume = order.Volume * p.Value / totalWeight;

						return newOrder;
					})
					.ToArray();

				return orders;
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str3289Key)]
		[DescriptionLoc(LocalizedStrings.Str3290Key)]
		private sealed class FreeMoneyPortfolio : WeighedVirtualPortfolio
		{
			private readonly UIStrategy _strategy;

			public FreeMoneyPortfolio(UIStrategy strategy)
			{
				if (strategy == null)
					throw new ArgumentNullException(nameof(strategy));

				_strategy = strategy;
			}

			public override IEnumerable<Order> SplitOrder(Order order)
			{
				var orders = InnerPortfolios
					.Select(p =>
					{
						var newOrder = order.ReRegisterClone(newVolume: order.Balance);

						var price = order.Security.GetCurrentPrice(_strategy, Sides.Buy, MarketPriceTypes.Middle);

						if (price == null)
							return null;

						newOrder.Portfolio = p.Key;
						newOrder.Volume = p.Key.CurrentValue * p.Value / (decimal)price;

						return newOrder;
					})
					.Where(o => o != null)
					.ToArray();

				return orders;
			}
		}

		//private readonly IEnumerable<IStudioControl> _controls;
		private readonly UserPortfolioControl _portfolioControl;
		private WeighedVirtualPortfolio _virtualPortfolio;

		public WeighedVirtualPortfolio VirtualPortfolio
		{
			get { return _virtualPortfolio; }
			set
			{
				_virtualPortfolio = value;
				_portfolioControl.VirtualPortfolio = value;
			}
		}

		//IEnumerable<IStudioControl> IUIProvider.Controls
		//{
		//	get { return _controls; }
		//}

		public UIStrategy()
		{
			_portfolioControl = new UserPortfolioControl();
			_portfolioControl.SelectedPortfolioTypeChanged += type =>
			{
				VirtualPortfolio = CreatePortfolio(type, VirtualPortfolio != null ? VirtualPortfolio.Save() : null);
			};
			_portfolioControl.VirtualPortfolioTypes.AddRange(new[]
			{
				GetDescription<SharedPortfolio>(),
				GetDescription<FreeMoneyPortfolio>()
			});

			//_controls = new[] { (IStudioControl)_portfolioControl, new UIStrategyControl(this)  };
		}

		public override void RegisterOrder(Order order)
		{
			if (_portfolioControl.UseVirtualPortfolio && order.Portfolio == Portfolio)
			{
				VirtualPortfolio
					.SplitOrder(order)
					.ForEach(RegisterOrder);
			}

			base.RegisterOrder(order);
		}

		public override void Load(SettingsStorage storage)
		{
			var virtualPortfolio = storage.GetValue<SettingsStorage>("VirtualPortfolio");
			if (virtualPortfolio != null)
			{
				VirtualPortfolio = CreatePortfolio(virtualPortfolio.GetValue<Type>("type"), virtualPortfolio.GetValue<SettingsStorage>("settings"));
			}

			base.Load(storage);
		}

		public override void Save(SettingsStorage storage)
		{
			var type = VirtualPortfolio.GetType();

			storage.SetValue("VirtualPortfolio", new SettingsStorage
			{
				{ "type",  "{0}, {1}".Put(type.FullName, type.Assembly.GetName().Name) },
				{ "settings", VirtualPortfolio.Save() }
			});

			base.Save(storage);
		}

		private WeighedVirtualPortfolio CreatePortfolio(Type type, SettingsStorage settings)
		{
			var portfolio = type.CreateInstance<WeighedVirtualPortfolio>();

			if (settings != null)
				portfolio.Load(settings);

			return portfolio;
		}

		private static Tuple<Type, string, string> GetDescription<T>()
		{
			var type = typeof(T);
			
			return Tuple.Create(type, type.GetDisplayName(), type.GetDescription());
		}
	}
}