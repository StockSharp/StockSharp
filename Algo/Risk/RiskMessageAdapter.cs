namespace StockSharp.Algo.Risk
{
	using System;

	using Ecng.ComponentModel;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically controlling risk rules.
	/// </summary>
	public class RiskMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RiskMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		public RiskMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private IRiskManager _riskManager = new RiskManager();

		/// <summary>
		/// Risk control manager.
		/// </summary>
		public IRiskManager RiskManager
		{
			get { return _riskManager; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_riskManager = value;
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			ProcessRisk(message);
			InnerAdapter.SendInMessage(message);
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			ProcessRisk(message);

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void ProcessRisk(Message message)
		{
			foreach (var rule in RiskManager.ProcessRules(message))
			{
				InnerAdapter.AddWarningLog(LocalizedStrings.Str855Params,
					rule.GetType().GetDisplayName(), rule.Title, rule.Action);

				switch (rule.Action)
				{
					case RiskActions.ClosePositions:
					{
						break;
					}
					case RiskActions.StopTrading:
						InnerAdapter.SendInMessage(new DisconnectMessage());
						break;
					case RiskActions.CancelOrders:
						InnerAdapter.SendInMessage(new OrderGroupCancelMessage { TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId() });
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Create a copy of <see cref="RiskMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new RiskMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}