namespace StockSharp.Algo.Derivatives
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.Positions;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Portfolio model for calculating the values of Greeks by the Black-Scholes formula.
	/// </summary>
	public class BasketBlackScholes : BlackScholes
	{
		/// <summary>
		/// The model for calculating Greeks values by the Black-Scholes formula based on the position.
		/// </summary>
		public class InnerModel
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="InnerModel"/>.
			/// </summary>
			/// <param name="model">The model for calculating Greeks values by the Black-Scholes formula.</param>
			/// <param name="positionManager">The position manager.</param>
			public InnerModel(BlackScholes model, IPositionManager positionManager)
			{
				if (model == null)
					throw new ArgumentNullException("model");

				if (positionManager == null)
					throw new ArgumentNullException("positionManager");

				Model = model;
				PositionManager = positionManager;
			}

			/// <summary>
			/// The model for calculating Greeks values by the Black-Scholes formula.
			/// </summary>
			public BlackScholes Model { get; private set; }

			/// <summary>
			/// The position manager.
			/// </summary>
			public IPositionManager PositionManager { get; private set; }
		}

		/// <summary>
		/// The interface describing the internal models collection <see cref="BasketBlackScholes.InnerModels"/>.
		/// </summary>
		public interface IInnerModelList : ISynchronizedCollection<InnerModel>
		{
			/// <summary>
			/// To get the model for calculating Greeks values by the Black-Scholes formula for a particular option.
			/// </summary>
			/// <param name="option">Options contract.</param>
			/// <returns>The model. If the option is not registered, then <see langword="null" /> will be returned.</returns>
			InnerModel this[Security option] { get; }
		}

		private sealed class InnerModelList : CachedSynchronizedList<InnerModel>, IInnerModelList
		{
			private readonly BasketBlackScholes _parent;

			public InnerModelList(BasketBlackScholes parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
			}

			InnerModel IInnerModelList.this[Security option]
			{
				get
				{
					if (option == null)
						throw new ArgumentNullException("option");

					return this.SyncGet(c => c.FirstOrDefault(i => i.Model.Option == option));
				}
			}

			protected override bool OnAdding(InnerModel item)
			{
				item.Model.RoundDecimals = _parent.RoundDecimals;
				return base.OnAdding(item);
			}

			protected override bool OnInserting(int index, InnerModel item)
			{
				item.Model.RoundDecimals = _parent.RoundDecimals;
				return base.OnInserting(index, item);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketBlackScholes"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		public BasketBlackScholes(ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
			: base(securityProvider, dataProvider)
		{
			_innerModels = new InnerModelList(this);
		}

		private readonly InnerModelList _innerModels;

		/// <summary>
		/// Information about options.
		/// </summary>
		public IInnerModelList InnerModels
		{
			get { return _innerModels; }
		}

		/// <summary>
		/// The position by the underlying asset.
		/// </summary>
		public IPositionManager UnderlyingAssetPosition { get; set; }

		/// <summary>
		/// Options contract.
		/// </summary>
		public override Security Option
		{
			get { throw new NotSupportedException(); }
		}

		private Security _underlyingAsset;

		/// <summary>
		/// Underlying asset.
		/// </summary>
		public override Security UnderlyingAsset
		{
			get
			{
				if (_underlyingAsset == null)
				{
					var info = _innerModels.SyncGet(c => c.FirstOrDefault());

					if (info == null)
						throw new InvalidOperationException(LocalizedStrings.Str700);

					_underlyingAsset = info.Model.Option.GetAsset(SecurityProvider);
				}

				return _underlyingAsset;
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
					_innerModels.ForEach(m => m.Model.RoundDecimals = value);
				}
			}
		}

		private decimal GetAssetPosition()
		{
			return (UnderlyingAssetPosition != null ? UnderlyingAssetPosition.Position : 0);
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
			return ProcessOptions(bs => bs.Delta(currentTime, deviation, assetPrice)) + GetAssetPosition();
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
				var iv = (decimal?)DataProvider.GetSecurityValue(m.Model.Option, Level1Fields.ImpliedVolatility);
				return iv == null ? null : func(m.Model) * (usePos ? m.PositionManager.Position : 1);
			});
		}
	}
}