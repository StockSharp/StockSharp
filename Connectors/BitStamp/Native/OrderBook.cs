namespace StockSharp.BitStamp.Native
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	class OrderBook
	{
		[JsonProperty(PropertyName = "bids")]
		readonly List<double[]> _bids = new List<double[]>();
		[JsonIgnore]
		public IList<double[]> Bids { get { return _bids; } }

		[JsonProperty(PropertyName = "asks")]
		readonly List<double[]> _asks = new List<double[]>();
		[JsonIgnore]
		public IList<double[]> Asks { get { return _asks; } }

		//[JsonIgnore]
		//public int Count { get { return Math.Min(_asks.Count, _bids.Count); } }
	}

	//internal class OrderBook
	//{
	//	public OrderBook()
	//	{
	//		Bids = new Levels();
	//		Asks = new Levels();
	//	}

	//	public OrderBook(IEnumerable<Level> bids, IEnumerable<Level> asks)
	//	{
	//		Bids = new Levels(bids);
	//		Asks = new Levels(asks);
	//	}

	//	public Levels Bids { get; set; }
	//	public Levels Asks { get; set; }

	//	public override string ToString()
	//	{
	//		var sb = new StringBuilder();
	//		for (int i = 0; i < Math.Min(Bids.Count, Asks.Count); i++)
	//		{
	//			sb.AppendFormat("{0,15:###0.00000000} {1,15:########0.00}", Bids[i].Volume, Bids[i].Price);
	//			sb.Append(" | ");
	//			sb.AppendFormat("{0,15:###0.00000000} {1,15:########0.00}", Asks[i].Volume, Asks[i].Price);
	//			sb.AppendLine();
	//		}
	//		return sb.ToString();
	//	}
	//}

	//[DebuggerDisplay("Price = {Price}\tVolume = {Volume}")]
	//class Level
	//{
	//	public double Price { get; set; }
	//	public double Volume { get; set; }

	//	public double Value
	//	{
	//		get { return Price * Volume; }
	//	}

	//	public Aggregate Aggregate { get; set; }
	//}

	//class Levels : ICollection<Level>
	//{
	//	private readonly List<Level> _levels;

	//	public Levels()
	//	{
	//		_levels = new List<Level>();
	//	}

	//	public Levels(IEnumerable<Level> collection)
	//	{
	//		_levels = new List<Level>(collection);
	//	}

	//	public Level this[int index]
	//	{
	//		get { return _levels[index]; }
	//	}

	//	public void AddRange(IEnumerable<Level> collection)
	//	{
	//		foreach (var item in collection)
	//		{
	//			Add(item);
	//		}
	//	}

	//	public void Add(Level item)
	//	{
	//		item.Aggregate = item;
	//		if (Count != 0)
	//		{
	//			item.Aggregate += _levels[Count - 1].Aggregate;
	//		}
	//		_levels.Add(item);
	//	}

	//	public void Clear()
	//	{
	//		_levels.Clear();
	//	}

	//	public bool Contains(Level item)
	//	{
	//		return _levels.Contains(item);
	//	}

	//	public void CopyTo(Level[] array, int arrayIndex)
	//	{
	//		_levels.CopyTo(array, arrayIndex);
	//	}

	//	public bool Remove(Level item)
	//	{
	//		return _levels.Remove(item);
	//	}

	//	public int Count
	//	{
	//		get { return _levels.Count; }
	//	}

	//	public bool IsReadOnly
	//	{
	//		get { return false; }
	//	}

	//	public IEnumerator<Level> GetEnumerator()
	//	{
	//		return _levels.GetEnumerator();
	//	}

	//	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	//	{
	//		return _levels.GetEnumerator();
	//	}
	//}

	//class Aggregate
	//{
	//	public double Price { get; set; }
	//	public double Volume { get; set; }
	//	public double Value { get; set; }

	//	public static implicit operator Aggregate(Level value)
	//	{
	//		return new Aggregate
	//		{
	//			Price = value.Price,
	//			Volume = value.Volume,
	//			Value = value.Value,
	//		};
	//	}

	//	public static Aggregate operator +(Aggregate lhs, Aggregate rhs)
	//	{
	//		lhs.Volume += rhs.Volume;
	//		lhs.Value += rhs.Value;
	//		lhs.Price = (lhs.Volume / lhs.Value);
	//		return lhs;
	//	}
	//}
}