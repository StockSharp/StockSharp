namespace StockSharp.Algo.Derivatives;

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

	private sealed class InnerModelList(BasketBlackScholes parent) : CachedSynchronizedList<BlackScholes>, IInnerModelList
	{
		private readonly BasketBlackScholes _parent = parent ?? throw new ArgumentNullException(nameof(parent));

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
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public BasketBlackScholes(ISecurityProvider securityProvider, IMarketDataProvider dataProvider, IExchangeInfoProvider exchangeInfoProvider, IPositionProvider positionProvider)
		: base(securityProvider, dataProvider, exchangeInfoProvider)
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
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public BasketBlackScholes(Security underlyingAsset, IMarketDataProvider dataProvider, IExchangeInfoProvider exchangeInfoProvider, IPositionProvider positionProvider)
		: base(underlyingAsset, dataProvider, exchangeInfoProvider)
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

	/// <inheritdoc />
	public override Security Option => throw new NotSupportedException();

	/// <inheritdoc />
	public override Security UnderlyingAsset
	{
		get
		{
			if (base.UnderlyingAsset == null)
			{
				var model = _innerModels.SyncGet(c => c.FirstOrDefault());

				if (model == null)
					throw new InvalidOperationException(LocalizedStrings.ModelNoOptions);

				base.UnderlyingAsset = model.Option.GetAsset(SecurityProvider);
			}

			return base.UnderlyingAsset;
		}
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	public override decimal? Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		var pos = PositionProvider.Positions.Where(p => p.Security == UnderlyingAsset).Sum(p => p.CurrentValue);
		return ProcessOptions(bs => bs.Delta(currentTime, deviation, assetPrice)) + pos;
	}

	/// <inheritdoc />
	public override decimal? Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Gamma(currentTime, deviation, assetPrice));
	}

	/// <inheritdoc />
	public override decimal? Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Vega(currentTime, deviation, assetPrice));
	}

	/// <inheritdoc />
	public override decimal? Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Theta(currentTime, deviation, assetPrice));
	}

	/// <inheritdoc />
	public override decimal? Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Rho(currentTime, deviation, assetPrice));
	}

	/// <inheritdoc />
	public override decimal? Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Premium(currentTime, deviation, assetPrice));
	}

	/// <inheritdoc />
	public override decimal? ImpliedVolatility(DateTimeOffset currentTime, decimal premium)
	{
		return ProcessOptions(bs => bs.ImpliedVolatility(currentTime, premium), false);
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