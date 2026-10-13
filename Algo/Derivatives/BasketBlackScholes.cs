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
	public override decimal? Delta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		var pos = PositionProvider.Positions.Where(p => p.Security == UnderlyingAsset).Sum(p => p.CurrentValue);
		return ProcessOptions(bs => bs.Delta(currentTime, deviation, assetPrice), deviation, true) + pos;
	}

	/// <inheritdoc />
	public override decimal? Gamma(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Gamma(currentTime, deviation, assetPrice), deviation, true);
	}

	/// <inheritdoc />
	public override decimal? Vega(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Vega(currentTime, deviation, assetPrice), deviation, true);
	}

	/// <inheritdoc />
	public override decimal? Theta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Theta(currentTime, deviation, assetPrice), deviation, true);
	}

	/// <inheritdoc />
	public override decimal? Rho(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Rho(currentTime, deviation, assetPrice), deviation, true);
	}

	/// <inheritdoc />
	public override decimal? Premium(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return ProcessOptions(bs => bs.Premium(currentTime, deviation, assetPrice), deviation, true);
	}

	/// <inheritdoc />
	public override decimal? ImpliedVolatility(DateTime currentTime, decimal premium)
	{
		return ProcessOptions(bs => bs.ImpliedVolatility(currentTime, premium), null, false);
	}

	/// <summary>
	/// To sum a value over the options of the basket.
	/// </summary>
	/// <param name="func">The value of one option.</param>
	/// <param name="deviation">The standard deviation the caller supplied for the whole basket, or <see langword="null" /> when each option is priced by its own quoted volatility.</param>
	/// <param name="usePos">To weight each value by the position in that option.</param>
	/// <returns>The sum over the options the model can price.</returns>
	private decimal? ProcessOptions(Func<BlackScholes, decimal?> func, decimal? deviation, bool usePos)
	{
		return _innerModels.Cache.Sum(m =>
		{
			// An option is priced only when its volatility is known: the caller either states one for
			// the whole basket, or the provider quotes one for that option.
			if (deviation is null && DataProvider.GetSecurityValue(m.Option, Level1Fields.ImpliedVolatility) is null)
				return null;

			return func(m) * (usePos ? PositionProvider.Positions.Where(p => p.Security == m.Option).Sum(p => p.CurrentValue) : 1);
		});
	}
}
