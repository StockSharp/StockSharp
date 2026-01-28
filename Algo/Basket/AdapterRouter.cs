namespace StockSharp.Algo.Basket;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Default implementation of <see cref="IAdapterRouter"/>.
/// </summary>
public class AdapterRouter : IAdapterRouter
{
	private readonly Dictionary<MessageTypes, CachedSynchronizedSet<IMessageAdapter>> _messageTypeAdapters = [];
	private readonly SynchronizedDictionary<(SecurityId secId, DataType dt), IMessageAdapter> _securityAdapters = [];
	private readonly SynchronizedDictionary<string, IMessageAdapter> _portfolioAdapters = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<long, HashSet<IMessageAdapter>> _nonSupportedAdapters = [];

	private readonly IOrderRoutingState _orderRouting;
	private readonly Func<IMessageAdapter, IMessageAdapter> _getUnderlyingAdapter;
	private readonly CandleBuilderProvider _candleBuilderProvider;
	private readonly Func<bool> _levelExtend;

	/// <summary>
	/// Initializes a new instance of <see cref="AdapterRouter"/>.
	/// </summary>
	/// <param name="orderRouting">Order routing state.</param>
	/// <param name="getUnderlyingAdapter">Function to unwrap adapter to underlying.</param>
	/// <param name="candleBuilderProvider">Candle builder provider for subscription filtering.</param>
	/// <param name="levelExtend">Function returning Level1Extend setting.</param>
	public AdapterRouter(
		IOrderRoutingState orderRouting,
		Func<IMessageAdapter, IMessageAdapter> getUnderlyingAdapter,
		CandleBuilderProvider candleBuilderProvider,
		Func<bool> levelExtend)
	{
		_orderRouting = orderRouting ?? throw new ArgumentNullException(nameof(orderRouting));
		_getUnderlyingAdapter = getUnderlyingAdapter ?? throw new ArgumentNullException(nameof(getUnderlyingAdapter));
		_candleBuilderProvider = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));
		_levelExtend = levelExtend ?? throw new ArgumentNullException(nameof(levelExtend));
	}

	/// <inheritdoc />
	public (IMessageAdapter[] adapters, bool skipSupportedMessages) GetAdapters(Message message, Func<IMessageAdapter, IMessageAdapter> getWrapper)
	{
		var skipSupportedMessages = false;
		IMessageAdapter[] adapters = null;

		var adapter = message.Adapter;

		if (adapter != null)
			adapter = _getUnderlyingAdapter(adapter);

		if (adapter == null && message is MarketDataMessage mdMsg && mdMsg.DataType2.IsSecurityRequired && mdMsg.SecurityId != default)
		{
			adapter = _securityAdapters.TryGetValue((mdMsg.SecurityId, mdMsg.DataType2)) ?? _securityAdapters.TryGetValue((mdMsg.SecurityId, (DataType)null));

			if (adapter != null && !adapter.IsMessageSupported(message.Type))
			{
				adapter = null;
			}
		}

		if (adapter != null)
		{
			adapter = getWrapper(adapter);

			if (adapter != null)
			{
				adapters = [adapter];
				skipSupportedMessages = true;
			}
		}

		adapters ??= _messageTypeAdapters.TryGetValue(message.Type)?.Cache;

		if (adapters != null)
		{
			if (message.Type == MessageTypes.MarketData)
			{
				var mdMsg1 = (MarketDataMessage)message;
				var set = _nonSupportedAdapters.TryGetValue(mdMsg1.TransactionId);

				if (set != null)
				{
					adapters = [.. adapters.Where(a => !set.Contains(_getUnderlyingAdapter(a)))];
				}
				else if (mdMsg1.DataType2 == DataType.News && (mdMsg1.SecurityId == default || mdMsg1.SecurityId == SecurityId.News))
				{
					adapters = [.. adapters.Where(a => !a.IsSecurityNewsOnly)];
				}

				if (adapters.Length == 0)
					adapters = null;
			}
			else if (message.Type == MessageTypes.SecurityLookup)
			{
				var isAll = ((SecurityLookupMessage)message).IsLookupAll();

				if (isAll)
					adapters = [.. adapters.Where(a => a.IsSupportSecuritiesLookupAll())];
			}
			else if (message.Type == MessageTypes.OrderStatus)
			{
				if (!((ISubscriptionMessage)message).FilterEnabled)
					adapters = [.. adapters.Where(a => a.IsAllDownloadingSupported(DataType.Transactions))];
			}
			else if (message.Type == MessageTypes.PortfolioLookup)
			{
				if (!((ISubscriptionMessage)message).FilterEnabled)
					adapters = [.. adapters.Where(a => a.IsAllDownloadingSupported(DataType.PositionChanges))];
			}
		}

		return (adapters, skipSupportedMessages);
	}

	/// <inheritdoc />
	public IMessageAdapter[] GetSubscriptionAdapters(MarketDataMessage mdMsg, IMessageAdapter[] adapters, bool skipSupportedMessages)
	{
		return adapters.Where(a =>
		{
			if (skipSupportedMessages)
				return true;

			if (!mdMsg.DataType2.IsTFCandles)
			{
				var isCandles = mdMsg.DataType2.IsCandles;

				if (a.IsMarketDataTypeSupported(mdMsg.DataType2) && (!isCandles || a.IsCandlesSupported(mdMsg)))
					return true;
				else
				{
					if (mdMsg.DataType2 == DataType.MarketDepth)
					{
						if (mdMsg.BuildMode == MarketDataBuildModes.Load)
							return false;

						if (mdMsg.BuildFrom == DataType.Level1 || mdMsg.BuildFrom == DataType.OrderLog)
							return a.IsMarketDataTypeSupported(mdMsg.BuildFrom);
						else if (mdMsg.BuildFrom == null)
						{
							if (a.IsMarketDataTypeSupported(DataType.OrderLog))
								mdMsg.BuildFrom = DataType.OrderLog;
							else if (a.IsMarketDataTypeSupported(DataType.Level1))
								mdMsg.BuildFrom = DataType.Level1;
							else
								return false;

							return true;
						}

						return false;
					}
					else if (mdMsg.DataType2 == DataType.Level1)
						return _levelExtend() && a.IsMarketDataTypeSupported(mdMsg.BuildFrom ?? DataType.MarketDepth);
					else if (mdMsg.DataType2 == DataType.Ticks)
						return a.IsMarketDataTypeSupported(DataType.OrderLog);
					else
					{
						if (isCandles && a.TryGetCandlesBuildFrom(mdMsg, _candleBuilderProvider) != null)
							return true;

						return false;
					}
				}
			}

			var original = mdMsg.GetTimeFrame();
			var timeFrames = a.GetTimeFrames(mdMsg.SecurityId, mdMsg.From, mdMsg.To).ToArray();

			if (timeFrames.Contains(original) || a.CheckTimeFrameByRequest)
				return true;

			if (mdMsg.AllowBuildFromSmallerTimeFrame)
			{
				var smaller = timeFrames
							  .FilterSmallerTimeFrames(original)
							  .OrderByDescending()
							  .FirstOr();

				if (smaller != null)
					return true;
			}

			return a.TryGetCandlesBuildFrom(mdMsg, _candleBuilderProvider) != null;
		}).ToArray();
	}

	/// <inheritdoc />
	public IMessageAdapter GetPortfolioAdapter(string portfolioName, Func<IMessageAdapter, IMessageAdapter> getWrapper)
	{
		if (_portfolioAdapters.TryGetValue(portfolioName, out var adapter))
			return getWrapper(adapter);

		return null;
	}

	/// <inheritdoc />
	public bool TryGetOrderAdapter(long transactionId, out IMessageAdapter adapter)
		=> _orderRouting.TryGetOrderAdapter(transactionId, out adapter);

	/// <inheritdoc />
	public void AddOrderAdapter(long transactionId, IMessageAdapter adapter)
		=> _orderRouting.TryAddOrderAdapter(transactionId, _getUnderlyingAdapter(adapter));

	/// <inheritdoc />
	public void AddNotSupported(long transactionId, IMessageAdapter adapter)
	{
		var set = _nonSupportedAdapters.SafeAdd(transactionId, _ => []);
		set.Add(_getUnderlyingAdapter(adapter));
	}

	/// <inheritdoc />
	public void AddMessageTypeAdapter(MessageTypes type, IMessageAdapter adapter)
		=> _messageTypeAdapters.SafeAdd(type).Add(adapter);

	/// <inheritdoc />
	public void RemoveMessageTypeAdapter(MessageTypes type, IMessageAdapter adapter)
	{
		var list = _messageTypeAdapters.TryGetValue(type);

		if (list == null)
			return;

		list.Remove(adapter);

		if (list.Count == 0)
			_messageTypeAdapters.Remove(type);
	}

	/// <inheritdoc />
	public void SetSecurityAdapter(SecurityId secId, DataType dataType, IMessageAdapter adapter)
		=> _securityAdapters[(secId, dataType)] = adapter;

	/// <inheritdoc />
	public void RemoveSecurityAdapter(SecurityId secId, DataType dataType)
		=> _securityAdapters.Remove((secId, dataType));

	/// <inheritdoc />
	public void SetPortfolioAdapter(string portfolio, IMessageAdapter adapter)
		=> _portfolioAdapters[portfolio] = adapter;

	/// <inheritdoc />
	public void RemovePortfolioAdapter(string portfolio)
		=> _portfolioAdapters.Remove(portfolio);

	/// <inheritdoc />
	public void Clear()
	{
		_messageTypeAdapters.Clear();
		_nonSupportedAdapters.Clear();
	}

	/// <summary>
	/// Clear security adapters cache. Used during Load.
	/// </summary>
	public void ClearSecurityAdapters() => _securityAdapters.Clear();

	/// <summary>
	/// Clear portfolio adapters cache. Used during Load.
	/// </summary>
	public void ClearPortfolioAdapters() => _portfolioAdapters.Clear();

	/// <summary>
	/// Add security adapter mapping directly. Used during Load.
	/// </summary>
	public void AddSecurityAdapter((SecurityId, DataType) key, IMessageAdapter adapter)
		=> _securityAdapters.Add(key, adapter);

	/// <summary>
	/// Add portfolio adapter mapping directly. Used during Load.
	/// </summary>
	public void AddPortfolioAdapter(string key, IMessageAdapter adapter)
		=> _portfolioAdapters.Add(key, adapter);
}
