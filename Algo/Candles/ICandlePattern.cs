namespace StockSharp.Algo.Candles;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Localization;
using StockSharp.Messages;

/// <summary>
/// The interfaces describes candle pattern.
/// </summary>
public interface ICandlePattern : IPersistable
{
	/// <summary>
	/// Try recognize pattern.
	/// </summary>
	/// <param name="candle"><see cref="ICandleMessage"/>.</param>
	/// <returns>Check result.</returns>
	bool Recognize(ICandleMessage candle);
}

/// <summary>
/// Base implementation of <see cref="ICandlePattern"/>.
/// </summary>
public abstract class BaseCandlePattern : ICandlePattern
{
	/// <inheritdoc />
	public abstract bool Recognize(ICandleMessage candle);

	/// <inheritdoc />
	public virtual void Load(SettingsStorage storage)
	{
	}

	/// <inheritdoc />
	public virtual void Save(SettingsStorage storage)
	{
	}
}

/// <summary>
/// Flat candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.FlatCandleKey)]
public class CandleFlatPattern : BaseCandlePattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> candle.OpenPrice == candle.ClosePrice;
}

/// <summary>
/// White candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.WhiteCandleKey)]
public class CandleWhitePattern : BaseCandlePattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> candle.OpenPrice < candle.ClosePrice;
}

/// <summary>
/// Black candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.BlackCandleKey)]
public class CandleBlackPattern : BaseCandlePattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> candle.OpenPrice < candle.ClosePrice;
}

/// <summary>
/// Marubozu candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.MarubozuKey)]
public class CandleMarubozuPattern : BaseCandlePattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> candle.GetLength() == candle.GetBody();
}

/// <summary>
/// Spinning top candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.SpinningTopKey)]
public class CandleSpinningTopPattern : CandleMarubozuPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> !base.Recognize(candle) && (candle.GetBottomShadow() == candle.GetTopShadow());
}

/// <summary>
/// Hammer candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.HammerKey)]
public class CandleHammerPattern : CandleMarubozuPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> !base.Recognize(candle) && (candle.GetBottomShadow() == 0 || candle.GetTopShadow() == 0);
}

/// <summary>
/// Dragonfly candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.DragonflyKey)]
public class CandleDragonflyPattern : CandleFlatPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> base.Recognize(candle) && candle.GetTopShadow() == 0;
}

/// <summary>
/// Gravestone candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.GravestoneKey)]
public class CandleGravestonePattern : CandleFlatPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> base.Recognize(candle) && candle.GetBottomShadow() == 0;
}

/// <summary>
/// Bullish candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.BullishCandleKey)]
public class CandleBullishPattern : CandleWhitePattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> base.Recognize(candle) && candle.GetBottomShadow() >= candle.GetBody();
}

/// <summary>
/// Bearish candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.BearishCandleKey)]
public class CandleBearishPattern : CandleBlackPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
		=> base.Recognize(candle) && candle.GetTopShadow() >= candle.GetBody();
}