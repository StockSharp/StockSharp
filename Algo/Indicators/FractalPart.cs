namespace StockSharp.Algo.Indicators
{
	using System;

	using StockSharp.Localization;

	/// <summary>
	/// Part <see cref="Fractals"/>.
	/// </summary>
	[IndicatorHidden]
	[IndicatorOut(typeof(ShiftedIndicatorValue))]
	public class FractalPart : LengthIndicator<(decimal high, decimal low)>
	{
		private int _numCenter;

		private int _downTrendCounter;
		private int _upTrendCounter;
		private decimal? _extremum;
		private bool? _isUpTrend;

		/// <summary>
		/// Initializes a new instance of the <see cref="FractalPart"/>.
		/// </summary>
		/// <param name="isUp"><see cref="IsUp"/></param>
		public FractalPart(bool isUp)
		{
			IsUp = isUp;
		}

		/// <summary>
		/// Up value.
		/// </summary>
		public bool IsUp { get; }

		/// <inheritdoc />
		public override int Length
		{
			get => base.Length;
			set
			{
				if (value <= 2 || value % 2 == 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				base.Length = value;
			}
		}

		/// <inheritdoc />
		public override void Reset()
		{
			_downTrendCounter = _upTrendCounter = default;
			_isUpTrend = default;
			_extremum = default;
			_numCenter = Length / 2;

			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var (_, currHigh, currLow, _) = input.GetOhlc();

			var currValue = IsUp ? currHigh : currLow;

			if (Buffer.Count > 0)
			{
				var (prevHigh, prevLow) = Buffer.Back();

				var prevValue = IsUp ? prevHigh : prevLow;

				var isUpTrend = currValue == prevValue
					? (bool?)null
					: currValue > prevValue;

				void resetCounters()
				{
					_upTrendCounter = _downTrendCounter = default;
					_isUpTrend = default;
					_extremum = default;
				}

				void tryStart()
				{
					if (!input.IsFinal)
						return;

					resetCounters();

					if (isUpTrend is not null)
					{
						if (IsUp != isUpTrend.Value)
							return;

						_isUpTrend = isUpTrend.Value;

						if (isUpTrend.Value)
						{
							if (++_upTrendCounter == _numCenter)
							{
								_extremum = currValue;
								_isUpTrend = false;
							}
						}
						else
						{
							if (++_downTrendCounter == _numCenter)
							{
								_extremum = currValue;
								_isUpTrend = true;
							}
						}
					}
				}

				if (_isUpTrend is null)
				{
					tryStart();
				}
				else if (_isUpTrend == isUpTrend)
				{
					if (input.IsFinal)
					{
						if (isUpTrend.Value)
						{
							if (++_upTrendCounter == _numCenter)
							{
								if (_downTrendCounter == _numCenter)
								{
									var extremum = _extremum.Value;

									resetCounters();

									return new ShiftedIndicatorValue(this, extremum, _numCenter);
								}
								else
								{
									if (_downTrendCounter != default)
										throw new InvalidOperationException($"_downTrendCounter == {_downTrendCounter}");

									_extremum = currValue;
									_isUpTrend = false;
								}
							}
						}
						else
						{
							if (++_downTrendCounter == _numCenter)
							{
								if (_upTrendCounter == _numCenter)
								{
									var extremum = _extremum.Value;

									resetCounters();

									return new ShiftedIndicatorValue(this, extremum, _numCenter);
								}
								else
								{
									if (_upTrendCounter != default)
										throw new InvalidOperationException($"_upTrendCounter == {_upTrendCounter}");

									_extremum = currValue;
									_isUpTrend = true;
								}
							}
						}
					}
					else
					{
						if (isUpTrend.Value)
						{
							if ((_upTrendCounter + 1) == _numCenter)
							{
								if (_downTrendCounter == _numCenter)
								{
									return new ShiftedIndicatorValue(this, _extremum.Value, _numCenter);
								}
							}
						}
						else
						{
							if ((_downTrendCounter + 1) == _numCenter)
							{
								if (_upTrendCounter == _numCenter)
								{
									return new ShiftedIndicatorValue(this, _extremum.Value, _numCenter);
								}
							}
						}
					}
				}
				else
				{
					tryStart();
				}
			}

			if (input.IsFinal)
				Buffer.PushBack((currHigh, currLow));

			return new ShiftedIndicatorValue(this);
		}
	}
}
