namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	/// <summary>
	/// Historical message adapter.
	/// </summary>
	public interface IHistoryMessageAdapter : IMessageAdapter
	{
		/// <summary>
		/// The interval of message <see cref="TimeMessage"/> generation. By default, it is equal to 1 sec.
		/// </summary>
		TimeSpan MarketTimeChangedInterval { get; set; }

		/// <summary>
		/// Date in history for starting the paper trading.
		/// </summary>
		DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Date in history to stop the paper trading (date is included).
		/// </summary>
		DateTimeOffset StopDate { get; set; }

		/// <summary>
		/// Send next outgoing message.
		/// </summary>
		/// <returns><see langword="true" />, if message was sent, otherwise, <see langword="false" />.</returns>
		bool SendOutMessage();

		/// <summary>
		/// Send outgoing message and raise <see cref="IMessageChannel.NewOutMessage"/> event.
		/// </summary>
		/// <param name="message">Message.</param>
		void SendOutMessage(Message message);
	}

	/// <summary>
	/// Custom implementation of the <see cref="IHistoryMessageAdapter"/>.
	/// </summary>
	public class CustomHistoryMessageAdapter : MessageAdapterWrapper, IHistoryMessageAdapter
	{
		private readonly Queue<Message> _outMessages = new Queue<Message>();

		/// <summary>
		/// Initialize <see cref="CustomHistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public CustomHistoryMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public TimeSpan MarketTimeChangedInterval { get; set; }

		/// <inheritdoc />
		public DateTimeOffset StartDate { get; set; }

		/// <inheritdoc />
		public DateTimeOffset StopDate { get; set; }

		/// <inheritdoc />
		public bool SendOutMessage()
		{
			if (_outMessages.Count == 0)
				return false;

			SendOutMessage(_outMessages.Dequeue());
			return true;
		}

		/// <inheritdoc />
		public void SendOutMessage(Message message)
		{
			RaiseNewOutMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			_outMessages.Enqueue(message);
		}

		/// <summary>
		/// Create a copy of <see cref="CustomHistoryMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new CustomHistoryMessageAdapter(InnerAdapter);
		}
	}
}