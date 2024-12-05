namespace StockSharp.Algo.Indicators;

using System.Drawing;

using Ecng.Drawing;

/// <summary>
/// Indicator measures.
/// </summary>
public enum IndicatorMeasures
{
	/// <summary>
	/// Price.
	/// </summary>
	Price,

	/// <summary>
	/// 0 till 100.
	/// </summary>
	Percent,

	/// <summary>
	/// -1 till +1.
	/// </summary>
	MinusOnePlusOne,

	/// <summary>
	/// Volume.
	/// </summary>
	Volume,
}

/// <summary>
/// The interface describing indicator.
/// </summary>
public interface IIndicator : IPersistable, ICloneable<IIndicator>
{
	/// <summary>
	/// Unique ID.
	/// </summary>
	Guid Id { get; }

	/// <summary>
	/// Indicator name.
	/// </summary>
	string Name { get; set; }

	/// <summary>
	/// Whether the indicator is set.
	/// </summary>
	bool IsFormed { get; }

	/// <summary>
	/// Number of values that need to be processed in order for the indicator to initialize (be <see cref="IsFormed"/> equals <see langword="true"/>).
	/// <see langword="null"/> if undefined.
	/// </summary>
	int NumValuesToInitialize { get; }

	/// <summary>
	/// The container storing indicator data.
	/// </summary>
	IIndicatorContainer Container { get; }

	/// <summary>
	/// The indicator change event (for example, a new value is added).
	/// </summary>
	event Action<IIndicatorValue, IIndicatorValue> Changed;

	/// <summary>
	/// The event of resetting the indicator status to initial. The event is called each time when initial settings are changed (for example, the length of period).
	/// </summary>
	event Action Reseted;

	/// <summary>
	/// To handle the input value.
	/// </summary>
	/// <param name="input">The input value.</param>
	/// <returns>The new value of the indicator.</returns>
	IIndicatorValue Process(IIndicatorValue input);

	/// <summary>
	/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
	/// </summary>
	void Reset();

	/// <summary>
	/// <see cref="IndicatorMeasures"/>.
	/// </summary>
	IndicatorMeasures Measure { get; }

	/// <summary>
	/// Convert to indicator value.
	/// </summary>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	/// <param name="values"><see cref="IIndicatorValue.ToValues"/></param>
	/// <returns><see cref="IIndicatorValue"/></returns>
	IIndicatorValue CreateValue(DateTimeOffset time, object[] values);

	/// <summary>
	/// Chart indicator draw style.
	/// </summary>
	DrawStyles Style { get; }

	/// <summary>
	/// Indicator color. If <see langword="null"/> then the color will be automatically selected.
	/// </summary>
	Color? Color { get; }
}