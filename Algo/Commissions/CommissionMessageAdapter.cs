namespace StockSharp.Algo.Commissions
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically calculating commission.
	/// </summary>
	public class CommissionMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommissionMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public CommissionMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
			if (innerAdapter == null)
				throw new ArgumentNullException("innerAdapter");

			InnerAdapter.NewOutMessage += ProcessOutMessage;
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

		private ICommissionManager _commissionManager = new CommissionManager();

		/// <summary>
		/// The commission calculating manager.
		/// </summary>
		public ICommissionManager CommissionManager
		{
			get { return _commissionManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_commissionManager = value;
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			CommissionManager.Process(message);
			InnerAdapter.SendInMessage(message);
		}

		private void ProcessOutMessage(Message message)
		{
			var execMsg = message as ExecutionMessage;

			if (execMsg != null && (execMsg.ExecutionType == ExecutionTypes.Order || execMsg.ExecutionType == ExecutionTypes.Trade) && execMsg.Commission == null)
				execMsg.Commission = CommissionManager.Process(execMsg);

			_newOutMessage.SafeInvoke(message);
		}

		/// <summary>
		/// Create a copy of <see cref="CommissionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new CommissionMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}