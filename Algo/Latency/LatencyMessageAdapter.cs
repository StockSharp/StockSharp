#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Latency.Algo
File: LatencyMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		}

		private ILatencyManager _latencyManager = new LatencyManager();

		/// <summary>
		/// Orders registration delay calculation manager.
		/// </summary>
		public ILatencyManager LatencyManager
		{
			get => _latencyManager;
			set => _latencyManager = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			message.TryInitLocalTime(this);

			LatencyManager.ProcessMessage(message);

			return base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.Type == MessageTypes.Execution)
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.HasOrderInfo())
				{
					var latency = LatencyManager.ProcessMessage(execMsg);

					if (latency != null)
						execMsg.Latency = latency;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="LatencyMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new LatencyMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}