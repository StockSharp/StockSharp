#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Slippage.Algo
File: SlippageMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			get => _slippageManager;
			set => _slippageManager = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			if (message.IsBack)
			{
				base.SendInMessage(message);
				return;
			}

			SlippageManager.ProcessMessage(message);
			base.SendInMessage(message);
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (!message.IsBack)
			{
				var slippage = SlippageManager.ProcessMessage(message);

				if (slippage != null)
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.Slippage == null)
						execMsg.Slippage = slippage;
				}	
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