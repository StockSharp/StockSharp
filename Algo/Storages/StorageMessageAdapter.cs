namespace StockSharp.Algo.Storages;

/// <summary>
/// Storage based message adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StorageMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
/// <param name="storageProcessor">Storage processor.</param>
public class StorageMessageAdapter(IMessageAdapter innerAdapter, IStorageProcessor storageProcessor) : MessageAdapterWrapper(innerAdapter)
{
	private readonly IStorageProcessor _storageProcessor = storageProcessor ?? throw new ArgumentNullException(nameof(storageProcessor));

	/// <inheritdoc />
	public override IEnumerable<DataType> GetSupportedMarketDataTypes(SecurityId securityId, DateTime? from, DateTime? to)
	{
		var dataTypes = base.GetSupportedMarketDataTypes(securityId, from, to);

		var settings = _storageProcessor.Settings;

		var drive = settings.Drive ?? settings.StorageRegistry.DefaultDrive;

		if (drive != null)
			dataTypes = dataTypes.Concat(drive.GetAvailableDataTypes(default, settings.Format).Where(dt => dt.IsMarketData)).Distinct();

		return dataTypes;
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				_storageProcessor.Reset();
				await base.OnSendInMessageAsync(message, cancellationToken);
				return;

			case MessageTypes.MarketData:
			{
				MarketDataMessage forwardMessage = null;

				await foreach (var outMsg in _storageProcessor.ProcessMarketData((MarketDataMessage)message, cancellationToken).WithEnforcedCancellation(cancellationToken))
				{
					if (outMsg is MarketDataMessage md)
						forwardMessage = md;
					else
						await RaiseStorageMessage(outMsg, cancellationToken);
				}

				if (forwardMessage != null)
					await base.OnSendInMessageAsync(forwardMessage, cancellationToken);

				return;
			}

			default:
				await base.OnSendInMessageAsync(message, cancellationToken);
				return;
		}
	}

	private ValueTask RaiseStorageMessage(Message message, CancellationToken cancellationToken)
	{
		message.TryInitLocalTime(this);

		return RaiseNewOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="StorageMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new StorageMessageAdapter(InnerAdapter.TypedClone(), _storageProcessor);
	}
}