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
			_parent.Adopt(item);
			return base.OnAdding(item);
		}

		protected override bool OnInserting(int index, BlackScholes item)
		{
			_parent.Adopt(item);
			return base.OnInserting(index, item);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketBlackScholes"/>.
	/// </summary>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="positionProvider">The position provider.</param>
	/// <param name="expirationTime">Explicit options expiration moment for the whole basket. If <c>null</c>, models' own settings are used.</param>
	public BasketBlackScholes(IMarketDataProvider dataProvider, IPositionProvider positionProvider, DateTime? expirationTime = null)
		: base(dataProvider, expirationTime)
	{
		_innerModels = new InnerModelList(this);
		PositionProvider = positionProvider ?? throw new ArgumentNullException(nameof(positionProvider));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketBlackScholes"/> with explicit underlying.
	/// </summary>
	/// <param name="underlyingAsset">Underlying asset.</param>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="positionProvider">The position provider.</param>
	/// <param name="expirationTime">Explicit options expiration moment for the whole basket.</param>
	public BasketBlackScholes(Security underlyingAsset, IMarketDataProvider dataProvider, IPositionProvider positionProvider, DateTime? expirationTime = null)
		: base(underlyingAsset, dataProvider, expirationTime)
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

				base.UnderlyingAsset = model.UnderlyingAsset;
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

			using (_innerModels.EnterScope())
			{
				_innerModels.ForEach(m => m.RoundDecimals = value);
			}
		}
	}

	/// <inheritdoc />
	/// <remarks>The basket's own expiry moment prices every option of it to that moment.</remarks>
	public override DateTime? ExpirationTime
	{
		get => base.ExpirationTime;
		set
		{
			base.ExpirationTime = value;

			// Set by the base constructor before the options exist.
			if (_innerModels is null || value is null)
				return;

			using (_innerModels.EnterScope())
			{
				_innerModels.ForEach(m => m.ExpirationTime = value);
			}
		}
	}

	/// <inheritdoc />
	/// <remarks>A basket has a time line only when it is given an expiry moment of its own.</remarks>
	public override double? GetExpirationTimeLine(DateTime currentTime)
		=> ExpirationTime is { } expiration ? DerivativesHelper.GetExpirationTimeLine(expiration, currentTime) : null;

	// An option joining the basket takes the basket's rounding and, when the basket has one, its expiry moment.
	private void Adopt(BlackScholes model)
	{
		model.RoundDecimals = RoundDecimals;

		if (ExpirationTime is { } expiration)
			model.ExpirationTime = expiration;
	}

	/// <inheritdoc />
	/// <remarks>
	/// An option that has expired by <paramref name="currentTime"/> counts with the slope of what it pays there. The asset
	/// held counts one for one, as in <see cref="Premium"/>.
	/// </remarks>
	public override decimal? Delta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> ProcessOptions(bs => bs.IsExpired(currentTime) ? bs.PayoffDelta(assetPrice) : bs.Delta(currentTime, deviation, assetPrice)) + PositionOfAsset();

	/// <inheritdoc />
	public override decimal? Gamma(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> ProcessOptions(bs => bs.IsExpired(currentTime) ? 0 : bs.Gamma(currentTime, deviation, assetPrice));

	/// <inheritdoc />
	public override decimal? Vega(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> ProcessOptions(bs => bs.IsExpired(currentTime) ? 0 : bs.Vega(currentTime, deviation, assetPrice));

	/// <inheritdoc />
	public override decimal? Theta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> ProcessOptions(bs => bs.IsExpired(currentTime) ? 0 : bs.Theta(currentTime, deviation, assetPrice));

	/// <inheritdoc />
	public override decimal? Rho(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> ProcessOptions(bs => bs.IsExpired(currentTime) ? 0 : bs.Rho(currentTime, deviation, assetPrice));

	/// <inheritdoc />
	/// <remarks>
	/// An option that has expired by <paramref name="currentTime"/> counts with what it pays there, and the asset held
	/// with its price, so that the value of the book and its <see cref="Delta"/> describe the same position.
	/// </remarks>
	public override decimal? Premium(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> base.Premium(currentTime, deviation, assetPrice);

	internal override decimal? PremiumCore(DateTime currentTime, decimal? deviation, decimal? assetPrice)
	{
		var options = ProcessOptions(bs => bs.IsExpired(currentTime) ? bs.Payoff(assetPrice) : bs.PremiumCore(currentTime, deviation, assetPrice));
		var held = PositionOfAsset();

		if (held == 0)
			return options;

		return GetAssetPrice(assetPrice) is decimal price ? options + held * price : null;
	}

	/// <inheritdoc />
	/// <remarks>
	/// The volatility at which the basket's own premium is the premium given, whether the book gains with volatility or, short
	/// of it, loses. A book that does both as volatility rises may be priced so at more than one, and one of them is found.
	/// </remarks>
	public override decimal? ImpliedVolatility(DateTime currentTime, decimal premium, decimal? assetPrice = null)
		=> base.ImpliedVolatility(currentTime, premium, assetPrice);

	/// <summary>
	/// To sum a value over the options of the basket, each weighted by the position in it.
	/// </summary>
	/// <param name="func">The value of one option.</param>
	/// <returns>The sum; <see langword="null" /> when an option the book holds cannot be valued, for the sum of the others
	/// is not the book's.</returns>
	private decimal? ProcessOptions(Func<BlackScholes, decimal?> func)
	{
		var total = 0m;

		foreach (var model in _innerModels.Cache)
		{
			var position = PositionOf(model.Option);

			if (position == 0)
				continue;

			if (func(model) is not decimal value)
				return null;

			total += value * position;
		}

		return total;
	}

	private decimal PositionOf(Security security)
		=> PositionProvider.Positions.Where(p => p.Security == security).Sum(p => p.CurrentValue ?? 0);

	// The asset held beside the options; a basket given no asset of its own and holding no option knows of none.
	private decimal PositionOfAsset()
		=> (base.UnderlyingAsset ?? _innerModels.SyncGet(c => c.FirstOrDefault())?.UnderlyingAsset) is { } asset ? PositionOf(asset) : 0;
}
