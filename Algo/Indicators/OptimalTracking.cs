namespace StockSharp.Algo.Indicators;

/// <summary>
/// Optimal Tracking.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/optimal_tracking.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OptimalTrackingKey,
	Description = LocalizedStrings.OptimalTrackingDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/optimal_tracking.html")]
public sealed class OptimalTracking : LengthIndicator<decimal>
{
	private static readonly decimal _smoothConstant1 = (decimal)Math.Exp(-0.25);
	private static readonly decimal _smoothConstant = 1 - _smoothConstant1;

	private struct CalcBuffer
	{
		private decimal _lambda;
		private decimal _alpha;

		private decimal _value1Old;
		private decimal _value2Old;
		private decimal _resultOld;

		public decimal Calculate(OptimalTracking ind, IList<decimal> buff, decimal average, decimal halfRange)
		{
			if (ind.IsFormed)
			{
				//Сглаженное приращение ****************************************************************************
				var avgDiff = buff[buff.Count - 1] - buff[buff.Count - 2];
				var smoothDiff = _smoothConstant * avgDiff + _smoothConstant1 * _value1Old;
				_value1Old = smoothDiff;

				//Сглаженный Half Range *********************************************************************************
				var smoothRng = _smoothConstant * halfRange + _smoothConstant1 * _value2Old;
				_value2Old = smoothRng;

				//Tracking index ***********************************************************************************
				if (smoothRng != 0)
					_lambda = Math.Abs(smoothDiff / smoothRng);

				//Alfa для альфа фильтра ***************************************************************************
				_alpha = (-_lambda * _lambda + (decimal)Math.Sqrt((double)(_lambda * _lambda * _lambda * _lambda + 16 * _lambda * _lambda))) / 8;

				//Smoothed result **********************************************************************************
				var check2 = _alpha * average;
				var check3 = (1 - _alpha) * _resultOld;
				var result = check2 + check3;
				_resultOld = result;

				return result;
			}

			_value2Old = halfRange;
			_resultOld = average;

			return _resultOld;
		}
	}

	private CalcBuffer _buf;

	/// <summary>
	/// Initializes a new instance of the <see cref="OptimalTracking"/>.
	/// </summary>
	public OptimalTracking()
	{
		Length = 2; //только 2 т.к текущая и пред свеча.
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_buf = default;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var average = (candle.HighPrice + candle.LowPrice) / 2;
		var halfRange = (candle.HighPrice - candle.LowPrice) / 2;

		if (input.IsFinal)
			Buffer.PushBack(average);

		var buff = input.IsFinal ? Buffer : (IList<decimal>)[.. Buffer.Skip(Buffer.Count >= Length ? 1 : 0), average];

		var b = _buf;

		var result = b.Calculate(this, buff, average, halfRange);

		if (input.IsFinal)
			_buf = b;

		return result;
	}
}