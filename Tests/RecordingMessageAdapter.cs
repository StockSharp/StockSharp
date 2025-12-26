namespace StockSharp.Tests;

sealed class RecordingMessageAdapter : MessageAdapter
{
	public RecordingMessageAdapter(IdGenerator transactionIdGenerator = null)
		: base(transactionIdGenerator ?? new IncrementalIdGenerator())
	{
	}

	public List<Message> InMessages { get; } = [];

	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		InMessages.Add(message);
		return default;
	}

	public void EmitOut(Message message)
		=> SendOutMessage(message);

	public override IMessageAdapter Clone()
		=> new RecordingMessageAdapter(TransactionIdGenerator);
}
