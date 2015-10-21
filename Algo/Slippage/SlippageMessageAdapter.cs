namespace StockSharp.Algo.Slippage
{
	using System;

	using Ecng.Common;

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
			if (innerAdapter == null)
				throw new ArgumentNullException("innerAdapter");

			InnerAdapter.NewOutMessage += ProcessOutMessage;
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
					throw new ArgumentNullException("value");

				_slippageManager = value;
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			InnerAdapter.NewOutMessage -= ProcessOutMessage;
			base.Dispose();
		}

		private Action<Message> _newOutMessage;

		/// <summary>
		/// New message event.
		/// </summary>
		public override event Action<Message> NewOutMessage
		{
			add { _newOutMessage += value; }
			remove { _newOutMessage -= value; }
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

		private void ProcessOutMessage(Message message)
		{
			var slippage = SlippageManager.ProcessMessage(message);

			if (slippage != null)
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.Slippage == null)
					execMsg.Slippage = slippage;
			}

			_newOutMessage.SafeInvoke(message);
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