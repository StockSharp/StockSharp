namespace StockSharp.Algo.Indicators;

/// <summary>
/// Market Meanness Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MMIKey,
	Description = LocalizedStrings.MarketMeannessIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/market_meanness_index.html")]
public class MarketMeannessIndex : LengthIndicator<decimal>
{
	private int _priceChanges;
	private int _directionChanges;
	private int _prevDirection;

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketMeannessIndex"/>.
	/// </summary>
	public MarketMeannessIndex()
	{
		Length = 200;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (input.IsFinal)
		{
			if (Buffer.Count == Length)
			{
				var oldPrice = Buffer[0];
				UpdateChanges(oldPrice, Buffer[1], true);
			}

			Buffer.PushBack(price);

			if (Buffer.Count > 1)
			{
				UpdateChanges(Buffer[^2], price, false);
			}
		}

		if (IsFormed)
		{
			decimal mmi;

			if (input.IsFinal)
			{
				mmi = _priceChanges > 0 ? 100m * _directionChanges / _priceChanges : 0;
			}
			else
			{
				var tempPriceChanges = _priceChanges;
				var tempDirectionChanges = _directionChanges;

				if (Buffer.Count > 1 && price != Buffer[^1])
				{
					tempPriceChanges++;
					
					var tempDirection = Math.Sign(price - Buffer[^1]);

					if (tempDirection != _prevDirection && _prevDirection != 0)
						tempDirectionChanges++;
				}

				mmi = tempPriceChanges > 0 ? 100m * tempDirectionChanges / tempPriceChanges : 0;
			}

			return mmi;
		}

		return null;
	}

	private void UpdateChanges(decimal prevPrice, decimal currentPrice, bool isRemoving)
	{
		var currentDirection = Math.Sign(currentPrice - prevPrice);

		if (currentDirection != 0)
			_priceChanges += isRemoving ? -1 : 1;

		if (currentDirection != _prevDirection && _prevDirection != 0)
			_directionChanges += isRemoving ? -1 : 1;

		if (!isRemoving)
			_prevDirection = currentDirection;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_priceChanges = default;
		_directionChanges = default;
		_prevDirection = default;

		base.Reset();
	}
}