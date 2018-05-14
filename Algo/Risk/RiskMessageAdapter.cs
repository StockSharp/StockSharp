#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Risk.Algo
File: RiskMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Risk
{
	using System;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

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
			get => _riskManager;
			set
			{
				_riskManager = value ?? throw new ArgumentNullException(nameof(value));

				if (_riskManager.Parent != null)
					_riskManager.Parent = this;
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			if (message.IsBack)
			{
				if (message.Adapter == this)
				{
					message.Adapter = null;
					message.IsBack = false;
				}
				
				base.SendInMessage(message);
				return;
			}

			ProcessRisk(message);
			base.SendInMessage(message);
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (!message.IsBack)
				ProcessRisk(message);

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void ProcessRisk(Message message)
		{
			foreach (var rule in RiskManager.ProcessRules(message))
			{
				this.AddWarningLog(LocalizedStrings.Str855Params,
					rule.GetType().GetDisplayName(), rule.Title, rule.Action);

				switch (rule.Action)
				{
					case RiskActions.ClosePositions:
					{
						break;
					}
					//case RiskActions.StopTrading:
					//	base.SendInMessage(new DisconnectMessage());
					//	break;
					case RiskActions.CancelOrders:
						RaiseNewOutMessage(new OrderGroupCancelMessage
						{
							TransactionId = TransactionIdGenerator.GetNextId(),
							IsBack = true,
							Adapter = this,
						});
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
			return new RiskMessageAdapter((IMessageAdapter)InnerAdapter.Clone())
			{
				RiskManager = RiskManager.Clone(),
			};
		}
	}
}