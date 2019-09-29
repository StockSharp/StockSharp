#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Latency.Algo
File: LatencyManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Latency
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Orders registration delay calculation manager.
	/// </summary>
	public class LatencyManager : ILatencyManager
	{
		private readonly SyncObject _syncObject = new SyncObject();
		private readonly Dictionary<long, DateTimeOffset> _register = new Dictionary<long, DateTimeOffset>();
		private readonly Dictionary<long, DateTimeOffset> _cancel = new Dictionary<long, DateTimeOffset>();

		/// <summary>
		/// Initializes a new instance of the <see cref="LatencyManager"/>.
		/// </summary>
		public LatencyManager()
		{
		}

		/// <inheritdoc />
		public virtual TimeSpan LatencyRegistration { get; private set; }

		/// <inheritdoc />
		public virtual TimeSpan LatencyCancellation { get; private set; }

		/// <inheritdoc />
		public TimeSpan? ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					Reset();
					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					lock (_syncObject)
					{
						AddRegister(regMsg.TransactionId, regMsg.LocalTime);
					}

					break;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;

					lock (_syncObject)
					{
						AddCancel(replaceMsg.TransactionId, replaceMsg.LocalTime);
						AddRegister(replaceMsg.TransactionId, replaceMsg.LocalTime);
					}

					break;
				}
				case MessageTypes.OrderPairReplace:
				{
					var replaceMsg = (OrderPairReplaceMessage)message;

					lock (_syncObject)
					{
						AddCancel(replaceMsg.Message1.TransactionId, replaceMsg.LocalTime);
						AddRegister(replaceMsg.Message1.TransactionId, replaceMsg.LocalTime);

						AddCancel(replaceMsg.Message2.TransactionId, replaceMsg.LocalTime);
						AddRegister(replaceMsg.Message2.TransactionId, replaceMsg.LocalTime);
					}

					break;
				}
				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					lock (_syncObject)
						AddCancel(cancelMsg.TransactionId, cancelMsg.LocalTime);

					break;
				}
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (!execMsg.HasOrderInfo())
						break;

					if (execMsg.OrderState == OrderStates.Pending)
						break;

					var transId = execMsg.OriginalTransactionId;

					lock (_syncObject)
					{
						if (!_register.TryGetValue(transId, out var time))
						{
							if (_cancel.TryGetValue(transId, out time))
							{
								_cancel.Remove(transId);

								if (execMsg.OrderState == OrderStates.Failed)
									break;

								return execMsg.LocalTime - time;
							}
						}
						else
						{
							_register.Remove(transId);

							if (execMsg.OrderState == OrderStates.Failed)
								break;

							return execMsg.LocalTime - time;
						}
					}

					break;
				}
			}

			return null;
		}

		private void AddRegister(long transactionId, DateTimeOffset localTime)
		{
			if (transactionId == 0)
				throw new ArgumentNullException(nameof(transactionId));

			if (localTime.IsDefault())
				throw new ArgumentNullException(nameof(localTime));

			if (_register.ContainsKey(transactionId))
				throw new ArgumentException(LocalizedStrings.Str1106Params.Put(transactionId), nameof(transactionId));

			_register.Add(transactionId, localTime);
		}

		private void AddCancel(long transactionId, DateTimeOffset localTime)
		{
			if (transactionId == 0)
				throw new ArgumentNullException(nameof(transactionId));

			if (localTime.IsDefault())
				throw new ArgumentNullException(nameof(localTime));

			if (_cancel.ContainsKey(transactionId))
				throw new ArgumentException(LocalizedStrings.Str1107Params.Put(transactionId), nameof(transactionId));

			_cancel.Add(transactionId, localTime);
		}

		/// <inheritdoc />
		public virtual void Reset()
		{
			LatencyRegistration = LatencyCancellation = TimeSpan.Zero;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Load(SettingsStorage storage)
		{
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Storage.</param>
		public void Save(SettingsStorage storage)
		{
		}
	}
}