namespace StockSharp.Algo.PnL
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically calculating profit-loss.
	/// </summary>
	public class PnLMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PnLMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public PnLMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
			if (innerAdapter == null)
				throw new ArgumentNullException("innerAdapter");

			InnerAdapter.NewOutMessage += ProcessOutMessage;
		}

		private IPnLManager _pnLManager = new PnLManager();

		/// <summary>
		/// The profit-loss manager.
		/// </summary>
		public IPnLManager PnLManager
		{
			get { return _pnLManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_pnLManager = value;
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
			PnLManager.ProcessMessage(message);
			InnerAdapter.SendInMessage(message);
		}

		private void ProcessOutMessage(Message message)
		{
			var info = PnLManager.ProcessMessage(message);

			if (info != null && info.PnL != 0)
				((ExecutionMessage)message).PnL = info.PnL;

			_newOutMessage.SafeInvoke(message);
		}

		/// <summary>
		/// Create a copy of <see cref="PnLMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new PnLMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}