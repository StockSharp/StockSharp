namespace StockSharp.Algo.Indicators;

/// <summary>
/// Buffer.
/// </summary>
public class LengthIndicatorBuffer<TItem> : CircularBuffer<TItem>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LengthIndicatorBuffer{TItem}"/>.
	/// </summary>
	/// <param name="capacity">Capacity.</param>
	public LengthIndicatorBuffer(int capacity)
		: base(capacity)
	{
		Reset(capacity);
	}

	/// <summary>
	/// Calc <see cref="Sum"/>.
	/// </summary>
	public IOperator<TItem> Operator { get; set; }

	/// <summary>
	/// Calc <see cref="Max"/>.
	/// </summary>
	public IComparer<TItem> MaxComparer { get; set; }

	/// <summary>
	/// Calc <see cref="Min"/>.
	/// </summary>
	public IComparer<TItem> MinComparer { get; set; }

	/// <summary>
	/// Max value.
	/// </summary>
	public NullableEx<TItem> Max { get; private set; } = new();

	/// <summary>
	/// Min value.
	/// </summary>
	public NullableEx<TItem> Min { get; private set; } = new();

	/// <summary>
	/// Sum of all elements in buffer.
	/// </summary>
	public TItem Sum { get; private set; }

	/// <summary>
	/// Sum of all elements in buffer without the first element.
	/// </summary>
	public TItem SumNoFirst => Count == 0 ? default : Operator.Subtract(Sum, this[0]);

	/// <summary>
	/// Add with <see cref="CircularBuffer{TItem}.Capacity"/> auto adjust.
	/// </summary>
	/// <param name="result">Value.</param>
	public void AddEx(TItem result)
	{
		var op = Operator;
		var maxComparer = MaxComparer;
		var minComparer = MinComparer;

		var recalcMax = false;
		var recalcMin = false;

		if (Count == Capacity)
		{
			if (op is not null)
				Sum = op.Subtract(Sum, this[0]);

			if (maxComparer?.Compare(Max.Value, this[0]) == 0)
				recalcMax = true;

			if (minComparer?.Compare(Min.Value, this[0]) == 0)
				recalcMin = true;
		}

		PushBack(result);

		if (op is not null)
			Sum = op.Add(Sum, result);

		if (maxComparer is not null)
		{
			if (recalcMax)
				Max.Value = this.Max(maxComparer);
			else if (!Max.HasValue || maxComparer?.Compare(Max.Value, result) < 0)
				Max.Value = result;
		}

		if (minComparer is not null)
		{
			if (recalcMin)
				Min.Value = this.Min(minComparer);
			else if (!Min.HasValue || minComparer?.Compare(Min.Value, result) > 0)
				Min.Value = result;
		}
	}

	/// <summary>
	/// Reset.
	/// </summary>
	public void Reset(int capacity)
	{
		Clear();
		Capacity = capacity;
		Sum = default;
		Max = new();
		Min = new();
	}
}
