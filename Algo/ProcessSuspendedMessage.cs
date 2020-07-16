namespace StockSharp.Algo
{
	using StockSharp.Messages;

	class ProcessSuspendedMessage : Message
	{
		public SecurityId SecurityId { get; }

		public ProcessSuspendedMessage(IMessageAdapter adapter, SecurityId securityId = default)
			: base(ExtendedMessageTypes.ProcessSuspended)
		{
			this.LoopBack(adapter);
			SecurityId = securityId;
		}

		public override Message Clone() => new ProcessSuspendedMessage(Adapter, SecurityId);
	}
}