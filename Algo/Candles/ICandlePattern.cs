namespace StockSharp.Algo.Candles;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Serialization;
using Ecng.Collections;

using StockSharp.Localization;
using StockSharp.Messages;

/// <summary>
/// The interfaces describes candle pattern.
/// </summary>
public interface ICandlePattern : IPersistable
{
	/// <summary>
	/// Reset.
	/// </summary>
	void Reset();

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
	/// <summary>
	/// Initializes a new instance of the <see cref="BaseCandlePattern"/>.
	/// </summary>
	protected BaseCandlePattern()
	{
	}

	/// <summary>
	/// Small error value.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.EpsilonKey,
			Description = LocalizedStrings.EpsilonDescKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
	public decimal Epsilon { get; set; }

	/// <summary>
	/// Determines the <paramref name="value"/> is less by absolute than <see cref="Epsilon"/>.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <returns>Check result.</returns>
	protected bool IsEpsilon(decimal value)
		=> value.Abs() <= Epsilon;

	/// <inheritdoc />
	public virtual void Reset() { }

	/// <inheritdoc />
	public abstract bool Recognize(ICandleMessage candle);

	/// <inheritdoc />
	public virtual void Load(SettingsStorage storage)
	{
		Epsilon = storage.GetValue<decimal>(nameof(Epsilon));
	}

	/// <inheritdoc />
	public virtual void Save(SettingsStorage storage)
	{
		storage.Set(nameof(Epsilon), Epsilon);
	}

	/// <inheritdoc />
	public override string ToString() => GetType().GetDisplayName();
}

/// <summary>
/// Base complex implementation of <see cref="ICandlePattern"/>.
/// </summary>
public class ComplexCandlePattern : BaseCandlePattern
{
	private int _last;

	/// <summary>
	/// Initializes a new instance of the <see cref="ComplexCandlePattern"/>.
	/// </summary>
	public ComplexCandlePattern()
	{
	}

	/// <summary>
	/// Inner patterns.
	/// </summary>
	public IList<ICandlePattern> Inner { get; } = new List<ICandlePattern>();

	/// <inheritdoc />
	public override void Reset() => _last = 0;

	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
	{
		var res = Inner[_last].Recognize(candle);

		if (res)
		{
			if (++_last < Inner.Count)
				return false;

			_last = 0;
			return true;
		}

		_last = 0;
		return false;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.Set(nameof(Inner), Inner.Select(i => i.SaveEntire(false)).ToArray());
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Inner.Clear();
		Inner.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Inner)).Select(i => i.LoadEntire<ICandlePattern>()));
	}

	/// <inheritdoc />
	public override string ToString() => Inner.Select(i => i.GetType().GetDisplayName()).JoinCommaSpace();
}

/// <summary>
/// Flat candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.FlatCandleKey)]
public class CandleFlatPattern : BaseCandlePattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
	{
		if (Epsilon == 0)
			return candle.OpenPrice == candle.ClosePrice;

		return IsEpsilon(candle.OpenPrice - candle.ClosePrice);
	}
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
	{
		if (Epsilon == 0)
			return candle.GetLength() == candle.GetBody();

		return IsEpsilon(candle.GetLength() - candle.GetBody());
	}
}

/// <summary>
/// Spinning top candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.SpinningTopKey)]
public class CandleSpinningTopPattern : CandleMarubozuPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
	{
		if (base.Recognize(candle))
			return false;

		if (Epsilon == 0)
			return candle.GetBottomShadow() == candle.GetTopShadow();

		return IsEpsilon(candle.GetBottomShadow() - candle.GetTopShadow());
	}
}

/// <summary>
/// Hammer candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.HammerKey)]
public class CandleHammerPattern : CandleMarubozuPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
	{
		if (base.Recognize(candle))
			return false;

		if (Epsilon == 0)
			return candle.GetTopShadow() == 0;

		return IsEpsilon(candle.GetTopShadow());
	}
}

/// <summary>
/// Inverted Hammer candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.InvertedHammerKey)]
public class CandleInvertedHammerPattern : CandleMarubozuPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
	{
		if (base.Recognize(candle))
			return false;

		if (Epsilon == 0)
			return candle.GetBottomShadow() == 0;

		return IsEpsilon(candle.GetBottomShadow());
	}
}

/// <summary>
/// Dragonfly candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.DragonflyKey)]
public class CandleDragonflyPattern : CandleFlatPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
	{
		if (!base.Recognize(candle))
			return false;

		if (Epsilon == 0)
			return candle.GetTopShadow() == 0;

		return IsEpsilon(candle.GetTopShadow());
	}
}

/// <summary>
/// Gravestone candle pattern.
/// </summary>
[DisplayNameLoc(LocalizedStrings.GravestoneKey)]
public class CandleGravestonePattern : CandleFlatPattern
{
	/// <inheritdoc />
	public override bool Recognize(ICandleMessage candle)
	{
		if (!base.Recognize(candle))
			return false;

		if (Epsilon == 0)
			return candle.GetBottomShadow() == 0;

		return IsEpsilon(candle.GetBottomShadow());
	}
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