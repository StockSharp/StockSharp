namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// Candle builder adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CandleBuilderMessageAdapter"/>.
/// </remarks>
public class CandleBuilderMessageAdapter : MessageAdapterWrapper
{
	private readonly CandleBuilderProvider _candleBuilderProvider;
	private readonly ICandleBuilderManager _manager;
	private readonly bool _cloneOutCandles;

	/// <summary>
	/// Initializes a new instance of the <see cref="CandleBuilderMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <param name="cloneOutCandles">Indicates whether outgoing candles should be cloned before emitting.</param>
	public CandleBuilderMessageAdapter(IMessageAdapter innerAdapter, CandleBuilderProvider candleBuilderProvider, bool cloneOutCandles = true)
		: base(innerAdapter)
	{
		_candleBuilderProvider = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));
		_cloneOutCandles = cloneOutCandles;
		_manager = new CandleBuilderManager(
			this,
			TransactionIdGenerator,
			this,
			sendFinishedCandlesImmediatelly: false,
			buffer: null,
			cloneOutCandles: _cloneOutCandles,
			_candleBuilderProvider);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CandleBuilderMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <param name="manager">Candle builder manager.</param>
	/// <param name="cloneOutCandles">Indicates whether outgoing candles should be cloned before emitting.</param>
	public CandleBuilderMessageAdapter(IMessageAdapter innerAdapter, CandleBuilderProvider candleBuilderProvider, ICandleBuilderManager manager, bool cloneOutCandles = true)
		: base(innerAdapter)
	{
		_candleBuilderProvider = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
		_cloneOutCandles = cloneOutCandles;
	}

	/// <summary>
	/// Send out finished candles when they received.
	/// </summary>
	public bool SendFinishedCandlesImmediatelly
	{
		get => _manager.SendFinishedCandlesImmediatelly;
		set => _manager.SendFinishedCandlesImmediatelly = value;
	}

	/// <summary>
	/// Storage buffer.
	/// </summary>
	public IStorageBuffer Buffer
	{
		get => _manager.Buffer;
		set => _manager.Buffer = value;
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (toInner, toOut) = await _manager.ProcessInMessageAsync(message, cancellationToken);

		if (toOut.Length > 0)
		{
			foreach (var sendOutMsg in toOut)
				await RaiseNewOutMessageAsync(sendOutMsg, cancellationToken);
		}

		if (toInner.Length > 0)
		{
			if (toInner.Length == 1)
				await base.OnSendInMessageAsync(toInner[0], cancellationToken);
			else
				await toInner.Select(sendInMsg => base.OnSendInMessageAsync(sendInMsg, cancellationToken)).WhenAll();
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (forward, extraOut) = await _manager.ProcessOutMessageAsync(message, cancellationToken);

		if (extraOut.Length > 0)
		{
			foreach (var extra in extraOut)
				await base.OnInnerAdapterNewOutMessageAsync(extra, cancellationToken);
		}

		if (forward != null)
			await base.OnInnerAdapterNewOutMessageAsync(forward, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="CandleBuilderMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new CandleBuilderMessageAdapter(InnerAdapter.TypedClone(), _candleBuilderProvider, _cloneOutCandles)
		{
			SendFinishedCandlesImmediatelly = SendFinishedCandlesImmediatelly,
			Buffer = Buffer,
		};
	}
}
