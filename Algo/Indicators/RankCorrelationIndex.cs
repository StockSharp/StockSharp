namespace StockSharp.Algo.Indicators;

/// <summary>
/// Rank Correlation Index (Spearman's Rank Correlation Coefficient).
/// Compares price movement ranking vs time ranking inside the window.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/rank_correlation_index.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RankCorrelationIndexKey,
	Description = LocalizedStrings.RankCorrelationIndexDescKey)]
[Doc("topics/api/indicators/list_of_indicators/rank_correlation_index.html")]
public class RankCorrelationIndex : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RankCorrelationIndex"/>.
	/// </summary>
	public RankCorrelationIndex()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		// Update buffer only on final values.
		if (input.IsFinal)
			Buffer.PushBack(newValue);

		// Build working window:
		//  - final: use current buffer
		//  - preview (non-final): emulate rolling by removing the oldest and appending new value
		IList<decimal> window;
		if (input.IsFinal)
		{
			window = Buffer;
		}
		else
		{
			if (Buffer.Count == 0)
				return new DecimalIndicatorValue(this, input.Time);
			
			var tmp = new decimal[Buffer.Count];
			int idx = 0;
			bool skipped = false;

			foreach (var v in Buffer)
			{
				if (!skipped) { skipped = true; continue; }
				tmp[idx++] = v;
			}

			if (idx < tmp.Length)
				tmp[idx] = newValue; // last slot

			window = tmp;
		}

		// Need full window before calculating.
		if (!IsFormed)
			return new DecimalIndicatorValue(this, input.Time);

		// Calculate ranks for prices (with tie handling via average ranks).
		var valueRanks = GetRanks(window);

		// Period (time) ranks are simply 1..Length in order.
		var n = valueRanks.Length;
		var periodRanks = new decimal[n];
		for (var i = 0; i < n; i++)
			periodRanks[i] = i + 1;

		// Spearman correlation (implemented as Pearson of the two rank arrays to handle ties correctly).
		var rankCorr = CalculateSpearmanCorrelation(valueRanks, periodRanks);

		return new DecimalIndicatorValue(this, rankCorr, input.Time);
	}

	/// <summary>
	/// Produce rank array with average rank for ties.
	/// </summary>
	private static decimal[] GetRanks(IList<decimal> values)
	{
		var n = values.Count;
		var ranks = new decimal[n];
		var indices = new int[n];
		for (var i = 0; i < n; i++) indices[i] = i;

		Array.Sort(indices, (a, b) => values[a].CompareTo(values[b]));

		for (var i = 0; i < n; i++)
		{
			var j = i;
			decimal sum = 0;
			int cnt = 0;
			var val = values[indices[i]];
			while (j < n && values[indices[j]].CompareTo(val) == 0)
			{
				sum += j + 1; // ranks start at 1
				cnt++;
				j++;
			}
			var avgRank = sum / cnt;
			for (var k = i; k < j; k++)
				ranks[indices[k]] = avgRank;
			i = j - 1;
		}

		return ranks;
	}

	/// <summary>
	/// Pearson correlation between two rank arrays (works with ties).
	/// </summary>
	private static decimal CalculateSpearmanCorrelation(decimal[] ranks1, decimal[] ranks2)
	{
		var n = ranks1.Length;
		if (n <= 1)
			return 0m;

		decimal mean1 = 0, mean2 = 0;
		for (var i = 0; i < n; i++) { mean1 += ranks1[i]; mean2 += ranks2[i]; }
		mean1 /= n; mean2 /= n;

		decimal num = 0, sumSq1 = 0, sumSq2 = 0;
		for (var i = 0; i < n; i++)
		{
			var d1 = ranks1[i] - mean1;
			var d2 = ranks2[i] - mean2;
			num += d1 * d2;
			sumSq1 += d1 * d1;
			sumSq2 += d2 * d2;
		}

		var den = (decimal)Math.Sqrt((double)(sumSq1 * sumSq2));
		if (den == 0)
			return 0m;
		return num / den;
	}
}