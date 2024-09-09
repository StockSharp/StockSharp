﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Vortex Indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.VIKey,
	Description = LocalizedStrings.VortexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/vortex_indicator.html")]
public class VortexIndicator : BaseComplexIndicator
{
	private readonly VortexPart _plusVi;
	private readonly VortexPart _minusVi;

	/// <summary>
	/// Initializes a new instance of the <see cref="VortexIndicator"/>.
	/// </summary>
	public VortexIndicator()
		: this(14)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VortexIndicator"/>.
	/// </summary>
	/// <param name="length">Period length.</param>
	public VortexIndicator(int length)
		: this(new(true) { Length = length }, new(false) { Length = length })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VortexIndicator"/>.
	/// </summary>
	/// <param name="plusVi">+VI part.</param>
	/// <param name="minusVi">-VI part.</param>
	public VortexIndicator(VortexPart plusVi, VortexPart minusVi)
		: base(plusVi, minusVi)
	{
		_plusVi = plusVi ?? throw new ArgumentNullException(nameof(plusVi));
		_minusVi = minusVi ?? throw new ArgumentNullException(nameof(minusVi));

		_plusVi.Name = "+VI";
		_minusVi.Name = "-VI";
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <summary>
	/// +VI part.
	/// </summary>
	[Browsable(false)]
	public VortexPart PlusVi => _plusVi;

	/// <summary>
	/// -VI part.
	/// </summary>
	[Browsable(false)]
	public VortexPart MinusVi => _minusVi;

	/// <summary>
	/// Period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => _plusVi.Length;
		set => _plusVi.Length = _minusVi.Length = value;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;
}

/// <summary>
/// The part of the Vortex indicator.
/// </summary>
[IndicatorHidden]
public class VortexPart : LengthIndicator<decimal>
{
	private readonly CircularBufferEx<decimal> _trBuffer;
	private readonly CircularBufferEx<decimal> _vmBuffer;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="VortexPart"/>.
	/// </summary>
	/// <param name="isPositive"><see cref="IsPositive"/></param>
	public VortexPart(bool isPositive)
	{
		_trBuffer = new(Length) { Operator = new DecimalOperator() };
		_vmBuffer = new(Length) { Operator = new DecimalOperator() };
		IsPositive = isPositive;
	}

	/// <summary>
	/// Is this the positive (+VI) or negative (-VI) part.
	/// </summary>
	public bool IsPositive { get; }

	/// <inheritdoc />
	public override void Reset()
	{
		_trBuffer.Clear();
		_vmBuffer.Clear();
		_trBuffer.Capacity = _vmBuffer.Capacity = Length;
		_prevHigh = 0;
		_prevLow = 0;
		_prevClose = 0;

		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevClose == 0)
		{
			if (input.IsFinal)
			{
				_prevHigh = candle.HighPrice;
				_prevLow = candle.LowPrice;
				_prevClose = candle.ClosePrice;
			}

			return new DecimalIndicatorValue(this, input.Time);
		}

		var tr = Math.Max(Math.Max(candle.HighPrice - candle.LowPrice, Math.Abs(candle.HighPrice - _prevClose)), Math.Abs(candle.LowPrice - _prevClose));
		var vm = IsPositive ? Math.Abs(candle.HighPrice - _prevLow) : Math.Abs(candle.LowPrice - _prevHigh);

		decimal result;

		if (input.IsFinal)
		{
			_trBuffer.PushBack(tr);
			_vmBuffer.PushBack(vm);

			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevClose = candle.ClosePrice;

			result = _trBuffer.Sum != 0 ? _vmBuffer.Sum / _trBuffer.Sum : 0;
		}
		else
		{
			var tempSumTr = _trBuffer.Sum - (_trBuffer.Count == Length ? _trBuffer[0] : 0) + tr;
			var tempSumVm = _vmBuffer.Sum - (_vmBuffer.Count == Length ? _vmBuffer[0] : 0) + vm;
			result = tempSumTr != 0 ? tempSumVm / tempSumTr : 0;
		}

		return new DecimalIndicatorValue(this, IsFormed ? result : 0, input.Time);
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _trBuffer.Count >= Length;
}