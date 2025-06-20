namespace StockSharp.Algo.Storages;

/// <summary>
/// Storage based message adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StorageMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
/// <param name="storageProcessor">Storage processor.</param>
public class StorageMessageAdapter(IMessageAdapter innerAdapter, StorageProcessor storageProcessor) : MessageAdapterWrapper(innerAdapter)
{
	private readonly StorageProcessor _storageProcessor = storageProcessor ?? throw new ArgumentNullException(nameof(storageProcessor));

	/// <inheritdoc />
	public override IEnumerable<DataType> GetSupportedMarketDataTypes(SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
	{
		var dataTypes = base.GetSupportedMarketDataTypes(securityId, from, to);

		var settings = _storageProcessor.Settings;

		var drive = settings.Drive ?? settings.StorageRegistry.DefaultDrive;

		if (drive != null)
			dataTypes = dataTypes.Concat(drive.GetAvailableDataTypes(default, settings.Format).Where(dt => dt.IsMarketData)).Distinct();

		return dataTypes;
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				_storageProcessor.Reset();
				return base.OnSendInMessage(message);

			case MessageTypes.MarketData:
				message = _storageProcessor.ProcessMarketData((MarketDataMessage)message, RaiseStorageMessage);
				return message == null || base.OnSendInMessage(message);

			default:
				return base.OnSendInMessage(message);
		}
	}

	private void RaiseStorageMessage(Message message)
	{
		message.TryInitLocalTime(this);

		RaiseNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="StorageMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new StorageMessageAdapter(InnerAdapter.TypedClone(), _storageProcessor);
	}
}