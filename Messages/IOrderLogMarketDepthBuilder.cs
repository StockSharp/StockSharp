namespace StockSharp.Messages;

/// <summary>
/// Base interface for order book builder.
/// </summary>
public interface IOrderLogMarketDepthBuilder
{
	/// <summary>
	/// Get snapshot.
	/// </summary>
	/// <param name="serverTime"><see cref="QuoteChangeMessage.ServerTime"/></param>
	/// <returns>Snapshot.</returns>
	QuoteChangeMessage GetSnapshot(DateTimeOffset serverTime);

	/// <summary>
	/// Process order log item.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns>Market depth.</returns>
	QuoteChangeMessage Update(ExecutionMessage item);
}

/// <summary>
/// Default implementation of <see cref="IOrderLogMarketDepthBuilder"/>.
/// </summary>
public class OrderLogMarketDepthBuilder : IOrderLogMarketDepthBuilder
{
	private readonly Dictionary<long, decimal> _ordersByNum = [];
	private readonly Dictionary<string, decimal> _ordersByString = new(StringComparer.InvariantCultureIgnoreCase);

	private readonly SortedList<decimal, QuoteChange> _bids = new(new BackwardComparer<decimal>());
	private readonly SortedList<decimal, QuoteChange> _asks = [];

	private readonly QuoteChangeMessage _depth;

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderLogMarketDepthBuilder"/>.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	public OrderLogMarketDepthBuilder(SecurityId securityId)
		: this(new QuoteChangeMessage { SecurityId = securityId, BuildFrom = DataType.OrderLog })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderLogMarketDepthBuilder"/>.
	/// </summary>
	/// <param name="depth">Messages containing quotes.</param>
	public OrderLogMarketDepthBuilder(QuoteChangeMessage depth)
	{
		_depth = depth ?? throw new ArgumentNullException(nameof(depth));
		_depth.State = QuoteChangeStates.SnapshotComplete;
		_depth.BuildFrom = DataType.OrderLog;

		foreach (var bid in depth.Bids)
			_bids.Add(bid.Price, bid);

		foreach (var ask in depth.Asks)
			_asks.Add(ask.Price, ask);
	}

	QuoteChangeMessage IOrderLogMarketDepthBuilder.GetSnapshot(DateTimeOffset serverTime)
	{
		var depth = _depth.TypedClone();

		depth.ServerTime = serverTime;
		depth.LocalTime = serverTime;
		depth.Bids = [.. _bids.Values];
		depth.Asks = [.. _asks.Values];

		return depth;
	}

	QuoteChangeMessage IOrderLogMarketDepthBuilder.Update(ExecutionMessage item)
	{
		if (item == null)
			throw new ArgumentNullException(nameof(item));

		if (item.DataType != DataType.OrderLog)
			throw new ArgumentException(item.ToString());

		if (item.OrderPrice == 0)
			return null;

		QuoteChange? changedQuote = null;

		var quotes = item.Side == Sides.Buy ? _bids : _asks;

		if (item.IsOrderLogRegistered())
		{
			if (item.OrderVolume != null)
			{
				QuoteChange ProcessRegister<T>(T id, Dictionary<T, decimal> orders)
				{
					var quote = quotes.SafeAdd(item.OrderPrice, key => new QuoteChange(key, 0));

					var volume = item.OrderVolume.Value;

					if (orders.TryGetValue(id, out var prevVolume))
					{
						quote.Volume += (volume - prevVolume);
						orders[id] = volume;
					}
					else
					{
						quote.Volume += volume;
						orders.Add(id, volume);
					}

					quotes[item.OrderPrice] = quote;
					return quote;
				}

				if (item.OrderId != null)
					changedQuote = ProcessRegister(item.OrderId.Value, _ordersByNum);
				else if (!item.OrderStringId.IsEmpty())
					changedQuote = ProcessRegister(item.OrderStringId, _ordersByString);
			}
		}
		else if (item.IsOrderLogMatched())
		{
			var volume = item.TradeVolume.Value;

			QuoteChange? ProcessMatched<T>(T id, Dictionary<T, decimal> orders)
			{
				if (orders.TryGetValue(id, out var prevVolume))
				{
					orders[id] = prevVolume - volume;

					if (quotes.TryGetValue(item.OrderPrice, out var quote))
					{
						quote.Volume -= volume;

						if (quote.Volume <= 0)
							quotes.Remove(item.OrderPrice);
						else
							quotes[item.OrderPrice] = quote;

						return quote;
					}
				}

				return null;
			}

			if (item.OrderId != null)
				changedQuote = ProcessMatched(item.OrderId.Value, _ordersByNum);
			else if (!item.OrderStringId.IsEmpty())
				changedQuote = ProcessMatched(item.OrderStringId, _ordersByString);
		}
		else if (item.IsOrderLogCanceled())
		{
			QuoteChange? ProcessCanceled<T>(T id, Dictionary<T, decimal> orders)
			{
				if (orders.TryGetAndRemove(id, out var prevVolume))
				{
					if (quotes.TryGetValue(item.OrderPrice, out var quote))
					{
						quote.Volume -= prevVolume;

						if (quote.Volume <= 0)
							quotes.Remove(item.OrderPrice);
						else
							quotes[item.OrderPrice] = quote;

						return quote;
					}
				}

				return null;
			}

			if (item.OrderId != null)
				changedQuote = ProcessCanceled(item.OrderId.Value, _ordersByNum);
			else if (!item.OrderStringId.IsEmpty())
				changedQuote = ProcessCanceled(item.OrderStringId, _ordersByString);
		}

		if (changedQuote == null)
			return null;

		_depth.ServerTime = item.ServerTime;
		_depth.LocalTime = item.LocalTime;

		var increment = new QuoteChangeMessage
		{
			ServerTime = item.ServerTime,
			LocalTime = item.LocalTime,
			SecurityId = _depth.SecurityId,
			State = QuoteChangeStates.Increment,
		};

		var q = changedQuote.Value;

		if (item.Side == Sides.Buy)
			increment.Bids = [q];
		else
			increment.Asks = [q];

		return increment;
	}
}
