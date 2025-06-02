namespace StockSharp.Algo.Indicators;

/// <summary>
/// Senkou (A) line.
/// </summary>
public class IchimokuSenkouALine : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="IchimokuSenkouALine"/>.
	/// </summary>
	/// <param name="tenkan">Tenkan line.</param>
	/// <param name="kijun">Kijun line.</param>
	public IchimokuSenkouALine(IchimokuLine tenkan, IchimokuLine kijun)
	{
		Tenkan = tenkan ?? throw new ArgumentNullException(nameof(tenkan));
		Kijun = kijun ?? throw new ArgumentNullException(nameof(kijun));

		Reset();
	}

	/// <summary>
	/// Tenkan line.
	/// </summary>
	[Browsable(false)]
	public IchimokuLine Tenkan { get; }

	/// <summary>
	/// Kijun line.
	/// </summary>
	[Browsable(false)]
	public IchimokuLine Kijun { get; }

	/// <inheritdoc />
	public override int Length
	{
		get => Kijun?.Length ?? 1;
		set => Kijun.Length = value;
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> Tenkan.NumValuesToInitialize.Max(Kijun.NumValuesToInitialize) + base.NumValuesToInitialize - 1;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		decimal? result = null;

		if (Tenkan.IsFormed && Kijun.IsFormed)
		{
			if (IsFormed || (input.IsFinal && Buffer.Count == (Length - 1)))
				result = Buffer[0];

			if (input.IsFinal)
				Buffer.PushBack((Tenkan.GetCurrentValue() + Kijun.GetCurrentValue()) / 2);
		}

		return result;
	}
}