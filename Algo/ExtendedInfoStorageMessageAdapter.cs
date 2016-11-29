namespace StockSharp.Algo
{
	using StockSharp.Algo.Storages;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, that save <see cref="Message.ExtensionInfo"/> into <see cref="IExtendedInfoStorage"/>.
	/// </summary>
	public class ExtendedInfoStorageMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessageAdapterWrapper"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="storage">Extended info <see cref="Message.ExtensionInfo"/> storage.</param>
		public ExtendedInfoStorageMessageAdapter(IMessageAdapter innerAdapter, IExtendedInfoStorageItem storage)
			: base(innerAdapter)
		{
			Storage = storage;
		}

		/// <summary>
		/// Extended info <see cref="Message.ExtensionInfo"/> storage.
		/// </summary>
		public IExtendedInfoStorageItem Storage { get; }

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			var secMsg = message as SecurityMessage;

			if (secMsg?.ExtensionInfo != null)
				Storage.Add(secMsg.SecurityId, secMsg.ExtensionInfo);

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="ExtendedInfoStorageMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new ExtendedInfoStorageMessageAdapter(InnerAdapter, Storage);
		}
	}
}