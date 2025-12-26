namespace StockSharp.Algo;

/// <summary>
/// Level1 depth builder adapter.
/// </summary>
public class Level1DepthBuilderAdapter : MessageAdapterWrapper
{
	private readonly ILevel1DepthBuilderManager _manager;

	/// <summary>
	/// Initializes a new instance of the <see cref="Level1DepthBuilderAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	public Level1DepthBuilderAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
		_manager = new Level1DepthBuilderManager(this);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Level1DepthBuilderAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	/// <param name="manager">Level1 depth builder manager.</param>
	public Level1DepthBuilderAdapter(IMessageAdapter innerAdapter, ILevel1DepthBuilderManager manager)
		: base(innerAdapter)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	/// <inheritdoc />
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (toInner, toOut) = _manager.ProcessInMessage(message);

		if (toInner.Length == 1 && toOut.Length == 0)
			return base.SendInMessageAsync(toInner[0], cancellationToken);

		return SendMultipleMessagesAsync(toInner, toOut, cancellationToken);
	}

	private async ValueTask SendMultipleMessagesAsync(Message[] toInner, Message[] toOut, CancellationToken cancellationToken)
	{
		foreach (var msg in toInner)
			await base.SendInMessageAsync(msg, cancellationToken);

		foreach (var msg in toOut)
			await RaiseNewOutMessageAsync(msg, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (forward, extraOut) = _manager.ProcessOutMessage(message);

		if (forward != null)
			await base.OnInnerAdapterNewOutMessageAsync(forward, cancellationToken);

		if (extraOut.Length > 0)
		{
			foreach (var extra in extraOut)
				await base.OnInnerAdapterNewOutMessageAsync(extra, cancellationToken);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="Level1DepthBuilderAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone() => new Level1DepthBuilderAdapter(InnerAdapter.TypedClone());
}
