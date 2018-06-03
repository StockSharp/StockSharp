#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Derivatives.Algo
File: BasketBlackScholes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Derivatives
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Portfolio model for calculating the values of Greeks by the Black-Scholes formula.
	/// </summary>
	public class BasketBlackScholes : BlackScholes
	{
		/// <summary>
		/// The interface describing the internal models collection <see cref="InnerModels"/>.
		/// </summary>
		public interface IInnerModelList : ISynchronizedCollection<BlackScholes>
		{
			/// <summary>
			/// To get the model for calculating Greeks values by the Black-Scholes formula for a particular option.
			/// </summary>
			/// <param name="option">Options contract.</param>
			/// <returns>The model. If the option is not registered, then <see langword="null" /> will be returned.</returns>
			BlackScholes this[Security option] { get; }
		}

		private sealed class InnerModelList : CachedSynchronizedList<BlackScholes>, IInnerModelList
		{
			private readonly BasketBlackScholes _parent;

			public InnerModelList(BasketBlackScholes parent)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

			BlackScholes IInnerModelList.this[Security option]
			{
				get
				{
					if (option == null)
						throw new ArgumentNullException(nameof(option));

					return this.SyncGet(c => c.FirstOrDefault(i => i.Option == option));
				}
			}

			protected override bool OnAdding(BlackScholes item)
			{
				item.RoundDecimals = _parent.RoundDecimals;
				return base.OnAdding(item);
			}

			protected override bool OnInserting(int index, BlackScholes item)
			{
				item.RoundDecimals = _parent.RoundDecimals;
				return base.OnInserting(index, item);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketBlackScholes"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <param name="positionProvider">The position provider.</param>
		public BasketBlackScholes(ISecurityProvider securityProvider, IMarketDataProvider dataProvider, IPositionProvider positionProvider)
			: base(securityProvider, dataProvider)
		{
			_innerModels = new InnerModelList(this);
			PositionProvider = positionProvider ?? throw new ArgumentNullException(nameof(positionProvider));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketBlackScholes"/>.
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <param name="positionProvider">The position provider.</param>
		public BasketBlackScholes(Security underlyingAsset, IMarketDataProvider dataProvider, IPositionProvider positionProvider)
			: base(underlyingAsset, dataProvider)
		{
			_innerModels = new InnerModelList(this);
			UnderlyingAsset = underlyingAsset;
			PositionProvider = positionProvider ?? throw new ArgumentNullException(nameof(positionProvider));
		}

		/// <summary>
		/// The position provider.
		/// </summary>
		public IPositionProvider PositionProvider { get; set; }

		private readonly InnerModelList _innerModels;

		/// <summary>
		/// Information about options.
		/// </summary>
		public IInnerModelList InnerModels => _innerModels;

		/// <summary>
		/// Options contract.
		/// </summary>
		public override Security Option => throw new NotSupportedException();

		/// <summary>
		/// Underlying asset.
		/// </summary>
		public override Security UnderlyingAsset
		{
			get
			{
				if (base.UnderlyingAsset == null)
				{
					var model = _innerModels.SyncGet(c => c.FirstOrDefault());

					if (model == null)
						throw new InvalidOperationException(LocalizedStrings.Str700);

					base.UnderlyingAsset = model.Option.GetAsset(SecurityProvider);
				}

				return base.UnderlyingAsset;
			}
		}

		/// <summary>
		/// The number of decimal places at calculated values. The default is -1, which means no values rounding.
		/// </summary>
		public override int RoundDecimals
		{
			set
			{
				base.RoundDecimals = value;

				lock (_innerModels.SyncRoot)
				{
					_innerModels.ForEach(m => m.RoundDecimals = value);
				}
			}
		}

		/// <summary>
		/// To calculate the option delta.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option delta. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			var pos = PositionProvider.Positions.Where(p => p.Security == UnderlyingAsset).Sum(p => p.CurrentValue);
			return ProcessOptions(bs => bs.Delta(currentTime, deviation, assetPrice)) + pos;
		}

		/// <summary>
		/// To calculate the option gamma.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option gamma. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Gamma(currentTime, deviation, assetPrice));
		}

		/// <summary>
		/// To calculate the option vega.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option vega. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Vega(currentTime, deviation, assetPrice));
		}

		/// <summary>
		/// To calculate the option theta.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option theta. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Theta(currentTime, deviation, assetPrice));
		}

		/// <summary>
		/// To calculate the option rho.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option rho. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Rho(currentTime, deviation, assetPrice));
		}

		/// <summary>
		/// To calculate the option premium.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option premium. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return ProcessOptions(bs => bs.Premium(currentTime, deviation, assetPrice));
		}

		/// <summary>
		/// To calculate the implied volatility.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="premium">The option premium.</param>
		/// <returns>The implied volatility. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? ImpliedVolatility(DateTimeOffset currentTime, decimal premium)
		{
			return ProcessOptions(bs => bs.ImpliedVolatility(currentTime, premium), false);
		}

		/// <summary>
		/// To create the order book of volatility.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <returns>The order book volatility.</returns>
		public override MarketDepth ImpliedVolatility(DateTimeOffset currentTime)
		{
			throw new NotSupportedException();
			//return UnderlyingAsset.GetMarketDepth().ImpliedVolatility(this);
		}

		private decimal? ProcessOptions(Func<BlackScholes, decimal?> func, bool usePos = true)
		{
			return _innerModels.Cache.Sum(m =>
			{
				var iv = (decimal?)DataProvider.GetSecurityValue(m.Option, Level1Fields.ImpliedVolatility);
				return iv == null ? null : func(m) * (usePos ? PositionProvider.Positions.Where(p => p.Security == m.Option).Sum(p => p.CurrentValue) : 1);
			});
		}
	}
}