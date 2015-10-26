namespace StockSharp.Algo.PnL
{
	using System;

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
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			PnLManager.ProcessMessage(message);

			base.SendInMessage(message);
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			var info = PnLManager.ProcessMessage(message);

			if (info != null && info.PnL != 0)
				((ExecutionMessage)message).PnL = info.PnL;

			base.OnInnerAdapterNewOutMessage(message);
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