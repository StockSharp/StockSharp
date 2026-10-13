namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// The net position that has to be held at the exchange per instrument, the one actually held there
/// on the hedging portfolio, and the hedges already sent to close the difference between them.
/// </summary>
/// <remarks>
/// Both sides of the subtraction meet here, so this is where the uncovered gap is measured against
/// <see cref="NetExposureLimit"/> and the order that closes it is produced. The gap is reserved when
/// that order is produced and not when the exchange answers, because nothing is heard about it in
/// between and an unreserved gap is what the next row would send a second order for. The reservation
/// is given back by the hedging position moving the way the hedge was sent, and by
/// <see cref="HedgeFinished"/> for what is left of a hedge that never arrived. Producing the order is
/// all this does - placing it, and any convention the order has to carry on the wire, belong to
/// whoever placed it.
/// </remarks>
public sealed class NetExposureHedger
{
	private sealed class Instrument
	{
		// The position that has to be at the exchange, and the one the hedging portfolio holds there.
		public decimal Exposure;
		public decimal Hedged;

		// Whether the exposure has been stated at all. Taking "not stated" for zero would read a
		// position that is wanted as an excess to be sold off.
		public bool Stated;

		// What each side states its own position is worth a unit; zero while none is stated.
		public decimal ExposurePrice;
		public decimal HedgedPrice;

		// The hedges produced and not accounted for yet, oldest first; signed, positive enlarges the
		// holding.
		public readonly List<(long transactionId, decimal signed)> Sent = [];

		// Signed volume standing in for a position that has not arrived.
		public decimal Reserved => Sent.Sum(t => t.signed);
	}

	private readonly IReadOnlyDictionary<SecurityId, NetExposureLimit> _limits;
	private readonly string _portfolioName;
	private readonly IdGenerator _idGenerator;

	private readonly Dictionary<SecurityId, Instrument> _bySecurity = [];
	private readonly Dictionary<long, SecurityId> _byTransaction = [];
	private readonly Lock _sync = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="NetExposureHedger"/>.
	/// </summary>
	/// <param name="limits">How far each instrument may run before its gap is closed. An instrument not named here is never hedged. The table is read on every row, so one named later is hedged from then on.</param>
	/// <param name="portfolioName">The portfolio a hedge is placed on, which has to reach the exchange.</param>
	/// <param name="idGenerator">Where the transaction id of a hedge order comes from.</param>
	/// <exception cref="ArgumentNullException"><paramref name="limits"/> or <paramref name="idGenerator"/> is <see langword="null"/>, or <paramref name="portfolioName"/> is empty.</exception>
	public NetExposureHedger(IReadOnlyDictionary<SecurityId, NetExposureLimit> limits, string portfolioName, IdGenerator idGenerator)
	{
		_limits = limits ?? throw new ArgumentNullException(nameof(limits));
		_portfolioName = portfolioName.ThrowIfEmpty(nameof(portfolioName));
		_idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
	}

	/// <summary>
	/// The portfolio a hedge is placed on, which has to reach the exchange.
	/// </summary>
	public string PortfolioName => _portfolioName;

	/// <summary>
	/// Whether any instrument is capped at all. Capping none is how the table says nothing is ever hedged.
	/// </summary>
	public bool IsHedging => _limits.Count > 0;

	/// <summary>
	/// How many capped instruments anything is known about. An instrument the table does not name is
	/// not one of them, whatever has been stated about it.
	/// </summary>
	public int Count
	{
		get
		{
			using (_sync.EnterScope())
				return _bySecurity.Count;
		}
	}

	/// <summary>
	/// States the net position that has to be held at the exchange on an instrument.
	/// </summary>
	/// <param name="securityId">The instrument.</param>
	/// <param name="volume">The signed position that has to be there.</param>
	/// <param name="averagePrice">What the exposure is worth a unit, or zero when nothing is known. The uncovered gap is what the money cap measures at this price.</param>
	/// <returns>The order that closes the gap, or <see langword="null"/> when none is due.</returns>
	public OrderRegisterMessage SetExposure(SecurityId securityId, decimal volume, decimal averagePrice)
	{
		using (_sync.EnterScope())
		{
			if (!Instrumented(securityId, out var instrument, out var limit))
				return null;

			instrument.Exposure = volume;
			instrument.Stated = true;
			instrument.ExposurePrice = averagePrice;

			return Decide(securityId, instrument, limit);
		}
	}

	/// <summary>
	/// States the net position the hedging portfolio actually holds at the exchange on an instrument.
	/// </summary>
	/// <remarks>
	/// A holding that moved the way a hedge was sent is that hedge arriving, and gives back what it
	/// reserved without waiting for <see cref="HedgeFinished"/>.
	/// </remarks>
	/// <param name="securityId">The instrument.</param>
	/// <param name="volume">The signed position that is there.</param>
	/// <param name="averagePrice">What the hedging portfolio holds it at a unit, or zero when nothing is known. This prices what is covered already, which the money cap does not measure.</param>
	/// <returns>The order that closes the gap, or <see langword="null"/> when none is due.</returns>
	public OrderRegisterMessage SetHedgePosition(SecurityId securityId, decimal volume, decimal averagePrice)
	{
		using (_sync.EnterScope())
		{
			if (!Instrumented(securityId, out var instrument, out var limit))
				return null;

			Arrived(instrument, volume - instrument.Hedged);

			instrument.Hedged = volume;
			instrument.HedgedPrice = averagePrice;

			return Decide(securityId, instrument, limit);
		}
	}

	/// <summary>
	/// A hedge order is no longer on its way - it filled, or it never reached the exchange.
	/// </summary>
	/// <remarks>
	/// Whatever the order still reserves goes back either way: a fill is already in the position the
	/// exchange reports, and one that failed was never there. Leaving the reservation would hold the
	/// gap open. The same transaction is accepted once, and a hedge the reported hedging position has
	/// already answered for reserves nothing by the time this is called.
	/// </remarks>
	/// <param name="transactionId">The transaction the hedge was produced under.</param>
	/// <returns><see langword="true"/> when the transaction was a hedge still reserving part of the gap.</returns>
	public bool HedgeFinished(long transactionId)
	{
		using (_sync.EnterScope())
		{
			if (!_byTransaction.Remove(transactionId, out var securityId))
				return false;

			if (_bySecurity.TryGetValue(securityId, out var instrument))
				instrument.Sent.RemoveAll(t => t.transactionId == transactionId);

			return true;
		}
	}

	// An instrument the table never names is never hedged, so nothing is kept about it. The table is
	// read on every row, so one named later starts here the way any instrument first heard of does.
	private bool Instrumented(SecurityId securityId, out Instrument instrument, out NetExposureLimit limit)
	{
		if (!_limits.TryGetValue(securityId, out limit))
		{
			instrument = null;
			return false;
		}

		if (!_bySecurity.TryGetValue(securityId, out instrument))
			_bySecurity[securityId] = instrument = new();

		return true;
	}

	// A hedge is reserved only to stand in for a position that has not arrived, so a holding that
	// moved the way the hedge was sent has replaced the stand-in with the thing itself.
	private void Arrived(Instrument instrument, decimal delta)
	{
		var sign = delta.Sign();

		if (sign == 0)
			return;

		var left = delta.Abs();

		for (var i = 0; i < instrument.Sent.Count && left > 0m;)
		{
			var (transactionId, signed) = instrument.Sent[i];

			// Movement answers only for hedges sent the same way; the rest of it came from elsewhere.
			if (signed.Sign() != sign)
			{
				i++;
				continue;
			}

			var taken = signed.Abs().Min(left);

			left -= taken;

			if (taken == signed.Abs())
			{
				_byTransaction.Remove(transactionId);
				instrument.Sent.RemoveAt(i);
			}
			else
				instrument.Sent[i] = (transactionId, signed - taken * sign);
		}
	}

	private OrderRegisterMessage Decide(SecurityId securityId, Instrument instrument, NetExposureLimit limit)
	{
		// Nothing to subtract from until the exposure has been stated.
		if (!instrument.Stated)
			return null;

		// What is still uncovered: what has to be there, less what is there and what is on its way.
		var gap = instrument.Exposure - instrument.Hedged - instrument.Reserved;
		var volume = gap.Abs();

		// The gap is the part of the exposure nothing covers yet, so it is worth what the exposure is
		// worth; what the hedging portfolio paid prices the part that is covered already.
		var price = instrument.ExposurePrice;

		// The notional cap is measured against what the gap is worth, and only while a price is
		// known. It caps the volume cap rather than replacing it: either one crossed closes the gap.
		var overNotional = limit.MaxNotional is decimal maxNotional
			&& price != 0m
			&& (gap * price).Abs() > maxNotional;

		if (volume <= limit.MaxVolume && !overNotional)
			return null;

		var transactionId = _idGenerator.GetNextId();

		_byTransaction.Add(transactionId, securityId);
		instrument.Sent.Add((transactionId, gap));

		return new OrderRegisterMessage
		{
			TransactionId = transactionId,
			SecurityId = securityId,
			PortfolioName = _portfolioName,
			Side = gap > 0 ? Sides.Buy : Sides.Sell,
			Volume = volume,
			OrderType = OrderTypes.Market,
			LocalTime = DateTime.UtcNow,
		};
	}
}
