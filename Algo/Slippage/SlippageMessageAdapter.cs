namespace StockSharp.Algo.Slippage
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically calculating slippage.
	/// </summary>
	public class SlippageMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SlippageMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public SlippageMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private ISlippageManager _slippageManager = new SlippageManager();

		/// <summary>
		/// Slippage manager.
		/// </summary>
		public ISlippageManager SlippageManager
		{
			get { return _slippageManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_slippageManager = value;
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			SlippageManager.ProcessMessage(message);
			InnerAdapter.SendInMessage(message);
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			var slippage = SlippageManager.ProcessMessage(message);

			if (slippage != null)
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.Slippage == null)
					execMsg.Slippage = slippage;
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="SlippageMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SlippageMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}