#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Commissions.Algo
File: CommissionMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		}

		private ICommissionManager _commissionManager = new CommissionManager();

		/// <summary>
		/// The commission calculating manager.
		/// </summary>
		public ICommissionManager CommissionManager
		{
			get => _commissionManager;
			set => _commissionManager = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			CommissionManager.Process(message);
			return base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message is ExecutionMessage execMsg && execMsg.ExecutionType == ExecutionTypes.Transaction && execMsg.Commission == null)
				execMsg.Commission = CommissionManager.Process(execMsg);

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="CommissionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new CommissionMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}