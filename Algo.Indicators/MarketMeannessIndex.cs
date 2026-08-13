namespace StockSharp.Algo.Indicators;

/// <summary>
/// Market Meanness Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MMIKey,
	Description = LocalizedStrings.MarketMeannessIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/market_meanness_index.html")]
public class MarketMeannessIndex : DecimalLengthIndicator
{
	private int _priceChanges;
	private int _directionChanges;

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
		var price = input.ToDecimal(Source);

		if (input.IsFinal)
		{
			if (Buffer.Count == Length)
				RemoveOldestDirection(ref _priceChanges, ref _directionChanges);

			Buffer.PushBack(price);

			if (Buffer.Count > 1)
				AddNewestDirection(ref _priceChanges, ref _directionChanges);
		}

		if (IsFormed)
		{
			var priceChanges = _priceChanges;
			var directionChanges = _directionChanges;

			if (!input.IsFinal)
			{
				if (Buffer.Count == Length)
					RemoveOldestDirection(ref priceChanges, ref directionChanges);

				AddPreviewDirection(price, ref priceChanges, ref directionChanges);
			}

			return priceChanges > 0 ? 100m * directionChanges / priceChanges : 0;
		}

		return null;
	}

	private void RemoveOldestDirection(ref int priceChanges, ref int directionChanges)
	{
		var removedDirection = GetDirection(Buffer[0], Buffer[1]);

		if (removedDirection != 0)
			priceChanges--;

		if (Buffer.Count > 2 && IsDirectionChange(removedDirection, GetDirection(Buffer[1], Buffer[2])))
			directionChanges--;
	}

	private void AddNewestDirection(ref int priceChanges, ref int directionChanges)
	{
		var direction = GetDirection(Buffer[^2], Buffer[^1]);
		var previousDirection = Buffer.Count > 2 ? GetDirection(Buffer[^3], Buffer[^2]) : (int?)null;

		AddDirection(direction, previousDirection, ref priceChanges, ref directionChanges);
	}

	private void AddPreviewDirection(decimal price, ref int priceChanges, ref int directionChanges)
	{
		var remainingPrices = Buffer.Count - (Buffer.Count == Length ? 1 : 0);
		var previousDirection = remainingPrices > 1 ? GetDirection(Buffer[^2], Buffer[^1]) : (int?)null;
		var direction = GetDirection(Buffer[^1], price);

		AddDirection(direction, previousDirection, ref priceChanges, ref directionChanges);
	}

	private static void AddDirection(int direction, int? previousDirection, ref int priceChanges, ref int directionChanges)
	{
		if (direction != 0)
			priceChanges++;

		if (previousDirection is int previous && IsDirectionChange(previous, direction))
			directionChanges++;
	}

	private static bool IsDirectionChange(int previousDirection, int direction)
		=> previousDirection != 0 && direction != previousDirection;

	private static int GetDirection(decimal previousPrice, decimal price)
		=> price.CompareTo(previousPrice);

	/// <inheritdoc />
	public override void Reset()
	{
		_priceChanges = default;
		_directionChanges = default;

		base.Reset();
	}
}
