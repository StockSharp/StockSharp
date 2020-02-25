namespace StockSharp.Algo.Storages
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// Buffered message adapter.
	/// </summary>
	public class BufferMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BufferMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="buffer">Storage buffer.</param>
		public BufferMessageAdapter(IMessageAdapter innerAdapter, StorageBuffer buffer)
			: base(innerAdapter)
		{
			Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
		}

		/// <summary>
		/// Storage buffer.
		/// </summary>
		public StorageBuffer Buffer { get; }

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				case MessageTypes.OrderRegister:
				case MessageTypes.OrderCancel:
				case MessageTypes.MarketData:
					Buffer.ProcessMessage(message);
					break;
			}

			return base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			Buffer.ProcessMessage(message);

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="BufferMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new BufferMessageAdapter((IMessageAdapter)InnerAdapter.Clone(), Buffer);
		}
	}
}