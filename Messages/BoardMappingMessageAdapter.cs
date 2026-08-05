namespace StockSharp.Messages;

/// <summary>
/// Presents a venue's instruments under this broker's own board names. Only the board is exchanged;
/// the instrument code stays as the venue spells it.
/// </summary>
/// <remarks>
/// The directions differ. Down there is one venue to send to, so an unlisted board goes to its
/// default; up nothing can be guessed, so a board only the venue names is shown as it is.
/// </remarks>
public class BoardMappingMessageAdapter : MessageAdapterWrapper
{
	private readonly IDictionary<string, string> _boards;
	private readonly string _defaultVenueBoard;

	// Case-insensitive lookup, configured spelling out.
	private readonly Dictionary<string, string> _toOwn;
	private readonly Dictionary<string, string> _toVenue;

	/// <summary>
	/// Initializes a new instance of the <see cref="BoardMappingMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter to wrap.</param>
	/// <param name="boards">Venue board to the board clients see, e.g. <c>BNB</c> to <c>SS</c>.</param>
	/// <param name="defaultVenueBoard">Where a board nobody listed is sent, e.g. <c>BNB</c>.</param>
	public BoardMappingMessageAdapter(IMessageAdapter innerAdapter, IDictionary<string, string> boards, string defaultVenueBoard)
		: base(innerAdapter)
	{
		_boards = boards ?? throw new ArgumentNullException(nameof(boards));
		_defaultVenueBoard = defaultVenueBoard.ThrowIfEmpty(nameof(defaultVenueBoard));

		if (_boards.Count == 0)
			throw new ArgumentException("No boards to map.", nameof(boards));

		_toOwn = new(StringComparer.OrdinalIgnoreCase);
		_toVenue = new(StringComparer.OrdinalIgnoreCase);

		foreach (var (venueBoard, ownBoard) in _boards)
		{
			if (venueBoard.IsEmpty() || ownBoard.IsEmpty())
				throw new ArgumentException("A board pair names both sides or neither.", nameof(boards));

			_toOwn.Add(venueBoard, ownBoard);
			_toVenue.Add(ownBoard, venueBoard);
		}
	}

	private SecurityId ToOwn(SecurityId id)
	{
		if (!id.BoardCode.IsEmpty() && _toOwn.TryGetValue(id.BoardCode, out var own))
			id.BoardCode = own;

		return id;
	}

	private SecurityId ToVenue(SecurityId id)
	{
		id.BoardCode = !id.BoardCode.IsEmpty() && _toVenue.TryGetValue(id.BoardCode, out var venue)
			? venue
			: _defaultVenueBoard;

		return id;
	}

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message is ISecurityIdMessage secIdMsg && !secIdMsg.SecurityId.SecurityCode.IsEmpty())
			secIdMsg.SecurityId = ToVenue(secIdMsg.SecurityId);

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message is ISecurityIdMessage secIdMsg && !secIdMsg.SecurityId.SecurityCode.IsEmpty())
			secIdMsg.SecurityId = ToOwn(secIdMsg.SecurityId);

		return base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	public override IMessageAdapter Clone()
		=> new BoardMappingMessageAdapter(InnerAdapter.TypedClone(), _boards, _defaultVenueBoard);
}
