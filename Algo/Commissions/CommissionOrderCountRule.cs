namespace StockSharp.Algo.Commissions;

/// <summary>
/// Number of orders commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderCountKey,
	Description = LocalizedStrings.OrderCountCommissionKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class CommissionOrderCountRule : CommissionRule
{
	private int _currentCount;
	private int _count = 1;
	private readonly HashSet<long> _transactionIds = [];
	private readonly HashSet<(SecurityId securityId, long orderId)> _orderIds = [];
	private readonly HashSet<(SecurityId securityId, string orderId)> _orderStringIds = [];

	// An order the venue reports again is reported again within moments, so a window of a few thousand
	// orders catches every repeat there is. Counted in entries rather than orders because one order is
	// remembered under each of the names it arrives with. Without a bound, a stand that runs for weeks
	// holds every order the venue ever reported: this is a charging rule, not a journal.
	private const int _maxRemembered = 10_000;

	private enum IdKinds
	{
		Transaction,
		Order,
		OrderString,
	}

	private readonly record struct Remembered(IdKinds Kind, long Number, SecurityId SecurityId, string StringId);

	// What is remembered, oldest first, so the oldest is what leaves when the window is full.
	private readonly Queue<Remembered> _remembered = [];

	private void Remember(Remembered entry)
	{
		_remembered.Enqueue(entry);

		while (_remembered.Count > _maxRemembered)
		{
			var oldest = _remembered.Dequeue();

			switch (oldest.Kind)
			{
				case IdKinds.Transaction:
					_transactionIds.Remove(oldest.Number);
					break;
				case IdKinds.Order:
					_orderIds.Remove((oldest.SecurityId, oldest.Number));
					break;
				case IdKinds.OrderString:
					_orderStringIds.Remove((oldest.SecurityId, oldest.StringId));
					break;
			}
		}
	}

	/// <summary>
	/// Order count.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrdersKey,
		Description = LocalizedStrings.OrdersCountKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Count
	{
		get => _count;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_count = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _count.To<string>();

	/// <inheritdoc />
	public override void Reset()
	{
		using (EnterScope())
		{
			_currentCount = 0;
			_transactionIds.Clear();
			_orderIds.Clear();
			_orderStringIds.Clear();
			_remembered.Clear();
		}

		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcess(ExecutionMessage message)
	{
		// Count only pure order messages. Own trades (partial fills) also carry order info,
		// but must not increase orders counter.
		if (!message.HasOrderInfo() || message.HasTradeInfo())
			return null;

		using (EnterScope())
		{
			var hasIdentity = message.OriginalTransactionId != 0 || message.OrderId is not null || !message.OrderStringId.IsEmpty();
			var alreadySeen =
				(message.OriginalTransactionId != 0 && _transactionIds.Contains(message.OriginalTransactionId)) ||
				(message.OrderId is long orderId && _orderIds.Contains((message.SecurityId, orderId))) ||
				(!message.OrderStringId.IsEmpty() && _orderStringIds.Contains((message.SecurityId, message.OrderStringId)));

			// Only what the set actually took is written down, or a repeat would push the window
			// along and forget an order that is still worth remembering.
			if (message.OriginalTransactionId != 0 && _transactionIds.Add(message.OriginalTransactionId))
				Remember(new(IdKinds.Transaction, message.OriginalTransactionId, default, default));

			if (message.OrderId is long orderId2 && _orderIds.Add((message.SecurityId, orderId2)))
				Remember(new(IdKinds.Order, orderId2, message.SecurityId, default));

			if (!message.OrderStringId.IsEmpty() && _orderStringIds.Add((message.SecurityId, message.OrderStringId)))
				Remember(new(IdKinds.OrderString, default, message.SecurityId, message.OrderStringId));

			if (hasIdentity && alreadySeen)
				return null;

			if (++_currentCount < Count)
				return null;

			_currentCount = 0;
			return (decimal)Value;
		}
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Count), Count);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Count = storage.GetValue<int>(nameof(Count));
	}
}
