namespace StockSharp.Algo.Latency
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically calculating network delays.
	/// </summary>
	public class LatencyMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LatencyMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public LatencyMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
			if (innerAdapter == null)
				throw new ArgumentNullException("innerAdapter");

			InnerAdapter.NewOutMessage += ProcessOutMessage;
		}

		private ILatencyManager _latencyManager = new LatencyManager();

		/// <summary>
		/// Orders registration delay calculation manager.
		/// </summary>
		public ILatencyManager LatencyManager
		{
			get { return _latencyManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_latencyManager = value;
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
			if (message.LocalTime.IsDefault())
				message.LocalTime = InnerAdapter.CurrentTime.LocalDateTime;

			LatencyManager.ProcessMessage(message);

			InnerAdapter.SendInMessage(message);
		}

		private void ProcessOutMessage(Message message)
		{
			ProcessExecution(message);

			_newOutMessage.SafeInvoke(message);
		}

		private void ProcessExecution(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return;

			var execMsg = (ExecutionMessage)message;

			if (execMsg.ExecutionType != ExecutionTypes.Order)
				return;

			var latency = LatencyManager.ProcessMessage(execMsg);

			if (latency != null)
				execMsg.Latency = latency;
		}

		/// <summary>
		/// Create a copy of <see cref="LatencyMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new LatencyMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}