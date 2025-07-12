namespace StockSharp.Algo.Indicators;

/// <summary>
/// Kalman Filter indicator.
/// </summary>
/// <remarks>
/// Adaptive filter for price smoothing and trend detection.
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KalmanFilterKey,
	Description = LocalizedStrings.KalmanFilterDescKey)]
[Doc("topics/api/indicators/list_of_indicators/kalman_filter.html")]
public class KalmanFilter : LengthIndicator<decimal>
{
	private decimal? _lastEstimate;
	private decimal _errorCovariance = 1m;

	private decimal _processNoise = 0.00001m;

	/// <summary>
	/// Process noise coefficient (Q).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ProcessNoiseKey,
		Description = LocalizedStrings.ProcessNoiseDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal ProcessNoise
	{
		get => _processNoise;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_processNoise = value;
			Reset();
		}
	}

	private decimal _measurementNoise = 0.001m;

	/// <summary>
	/// Measurement noise coefficient (R).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MeasurementNoiseKey,
		Description = LocalizedStrings.MeasurementNoiseDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal MeasurementNoise
	{
		get => _measurementNoise;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_measurementNoise = value;
			Reset();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="KalmanFilter"/> class.
	/// </summary>
	public KalmanFilter()
	{
		Length = 10;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var value = input.ToDecimal();

		if (_lastEstimate == null)
		{
			if (input.IsFinal)
			{
				_lastEstimate = value;
				_errorCovariance = 1m;

				Buffer.PushBack(value);
			}

			return value;
		}

		var priorEstimate = _lastEstimate.Value;
		var priorErrorCovariance = _errorCovariance + ProcessNoise;

		var kalmanGain = priorErrorCovariance / (priorErrorCovariance + MeasurementNoise);
		var newEstimate = priorEstimate + kalmanGain * (value - priorEstimate);

		if (input.IsFinal)
		{
			_errorCovariance = (1 - kalmanGain) * priorErrorCovariance;
			_lastEstimate = newEstimate;

			Buffer.PushBack(newEstimate);
		}

		return newEstimate;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_lastEstimate = null;
		_errorCovariance = 1m;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(ProcessNoise), ProcessNoise);
		storage.SetValue(nameof(MeasurementNoise), MeasurementNoise);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		ProcessNoise = storage.GetValue<decimal>(nameof(ProcessNoise));
		MeasurementNoise = storage.GetValue<decimal>(nameof(MeasurementNoise));
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()} P={ProcessNoise} M={MeasurementNoise}";
}