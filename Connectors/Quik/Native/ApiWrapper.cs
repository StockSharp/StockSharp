namespace StockSharp.Quik.Native
{
	using System;
	using System.IO;
	using System.Text;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.Messages;

	using StockSharp.Localization;

	enum Modes
	{
		New,
		Begin,
		End,
	}

	static class ApiResultHelper
	{
		public static Codes ToCode(this int res)
		{
			return (Codes)(res & 255);
		}

		public static void ThrowIfNeed(this int res, StringBuilder msg)
		{
			var code = res.ToCode();

			if (code != Codes.Success)
				throw new ApiException(code, msg.ToString());
		}
	}

	class ApiWrapper : Disposable
	{
		private readonly Api _api;

		public ApiWrapper(string dllPath)
		{
			_api = new Api(dllPath);

			var msg = new StringBuilder(_msgSize);
			var extEc = 0L;
			_api.SetConnectionStatusCallback(Marshaler.WrapDelegate<Api.ConnectionStatusCallback>(OnConnectionStatusCallback), ref extEc, msg, _msgSize).ThrowIfNeed(msg);

			msg = new StringBuilder(_msgSize);
			extEc = 0L;
			_api.SetTransactionsReply(Marshaler.WrapDelegate<Api.TransactionReplyCallback>(OnTransactionReplyCallback), ref extEc, msg, _msgSize);
		}

		public string DllPath
		{
			get { return _api.DllPath; }
		}

		private const int _msgSize = 256;

		#region Connect

		public void Connect(string path)
		{
			if (IsDllConnected() == Codes.DllConnected)
				return;

			var fileExists = false;
			try
			{
				fileExists = File.Exists(Path.Combine(path, "info.exe"));
			}
			catch (Exception ex)
			{
				ConnectionChanged.SafeInvoke(Codes.Failed, ex, LocalizedStrings.Str1721);
			}

			var msg = new StringBuilder(_msgSize);
			var extEc = 0L;

			try
			{
				_api.Connect(path, ref extEc, msg, _msgSize).ThrowIfNeed(msg);
			}
			catch (ApiException ex)
			{
				if (!fileExists)
					throw new ArgumentException(LocalizedStrings.Str1722Params.Put(path), "path", ex);
				else
					throw new ArgumentException(LocalizedStrings.Str1723, "path", ex);
			}

			//var c = IsDllConnected();

			//if (c != Codes.DllConnected)
			//    throw new ApiException(c, "Невозможно подключиться к quik терминалу.");

			//c = IsQuikConnected();

			//if (c != Codes.QuikConnected)
			//    throw new ApiException(c, "Невозможно подключиться к Quik серверу.");

			// если версия TRANS2QUIK 1.1
			if (_api.SubscribeOrders != null)
			{
				_api.SubscribeOrders(new StringBuilder(), new StringBuilder()).ThrowIfNeed(new StringBuilder());
				_api.SubscribeTrades(new StringBuilder(), new StringBuilder()).ThrowIfNeed(new StringBuilder());

				_api.StartOrders(Marshaler.WrapDelegate<Api.OrderStatusCallback>(OnStartOrders)).ThrowIfNeed(msg);
				_api.StartTrades(Marshaler.WrapDelegate<Api.TradeStatusCallback>(OnStartTrades)).ThrowIfNeed(msg);
			}
		}

		#endregion

		public bool IsConnected
		{
			get
			{
				return !IsDisposed && IsDllConnected() == Codes.DllConnected && IsQuikConnected() == Codes.QuikConnected;
			}
		}

		#region IsDllConnected

		public Codes IsDllConnected()
		{
			var msg = new StringBuilder(_msgSize);
			var extEc = 0L;
			return _api.IsDllConnected(ref extEc, msg, _msgSize).ToCode();
		}

		#endregion

		#region Disconnect

		public void Disconnect()
		{
			if (_api.SubscribeOrders != null)
			{
				_api.UnSubscribeOrders().ThrowIfNeed(new StringBuilder());
				_api.UnSubscribeTrades().ThrowIfNeed(new StringBuilder());
			}

			var msg = new StringBuilder(_msgSize);
			var extEc = 0L;
			_api.Disconnect(ref extEc, msg, _msgSize).ThrowIfNeed(msg);
		}

		#endregion

		#region IsQuikConnected

		public Codes IsQuikConnected()
		{
			var msg = new StringBuilder(_msgSize);
			var extEc = 0L;
			return _api.IsQuikConnected(ref extEc, msg, _msgSize).ToCode();
		}

		#endregion

		#region SendSyncTransaction

		public void SendSyncTransaction(string transactionTxt, out OrderStatus status, out uint transId, out long orderId, out string message)
		{
			System.Diagnostics.Debug.WriteLine(System.Threading.Thread.CurrentThread.GetHashCode() + " " + transactionTxt);

			var msg = new StringBuilder(_msgSize);
			var extEc = 0L;
			var transMsg = new StringBuilder(_msgSize);
			long statusL;
			double orderNum;
			var retVal = _api.SendSyncTransaction(transactionTxt, out statusL, out transId, out orderNum, transMsg, _msgSize, ref extEc, msg, _msgSize);
			orderId = (long)orderNum;
			retVal.ThrowIfNeed((retVal.ToCode() == Codes.WrongSyntax) ? transactionTxt.To<StringBuilder>() : msg);
			status = (OrderStatus)statusL;
			message = transMsg.ToString();
		}

		#endregion

		#region SendAsyncTransaction

		public void SendAsyncTransaction(string transactionTxt)
		{
			var msg = new StringBuilder(_msgSize);
			var extEc = 0L;
			_api.SendAsyncTransaction(transactionTxt, ref extEc, msg, _msgSize).ThrowIfNeed(msg);
		}

		#endregion

		#region ConnectionChanged

		public event Action<Codes, Exception, string> ConnectionChanged;

		private void OnConnectionStatusCallback(int connectionEvent, int extendedErrorCode, string infoMessage)
		{
			Exception error = null;

			var errorCode = extendedErrorCode.ToCode();

			if (errorCode != Codes.Success)
				error = new ApiException(errorCode, infoMessage);

			ConnectionChanged.SafeInvoke((Codes)connectionEvent, error, infoMessage);
		}

		#endregion

		#region TransactionReply

		public event Action<uint, Codes, Codes, OrderStatus, long, string> TransactionReply;

		private void OnTransactionReplyCallback(int transactionResult, int transactionExtendedErrorCode, int transactionReplyCode, uint transId, double orderNum, string transactionReplyMessage)
		{
			TransactionReply.SafeInvoke(transId, transactionResult.ToCode(), transactionExtendedErrorCode.ToCode(),
				(OrderStatus)transactionReplyCode, (long)orderNum, transactionReplyMessage);
		}

		#endregion

		#region OrderReply

		public event Action<Modes, uint, long, string, string, double, int, int, Sides, OrderStates> OrderReply;

		private void OnStartOrders(int mode, uint transId, double orderNum, string classCode, string secCode, double price, int balance, double volume, int direction, int status, int orderDescriptor)
		{
			OrderReply.SafeInvoke((Modes)mode, transId, (long)orderNum, classCode, secCode, price, balance, (int)volume, (Sides)direction, (OrderStates)status);
		}

		#endregion

		#region TradeReply

		public event Action<Modes, long, long, string, string, double, int, int, Sides> TradeReply;

		private void OnStartTrades(int mode, double tradeNum, double orderNum, string classCode, string secCode, double price, int balance, double volume, int direction, int tradeDescriptor)
		{
			TradeReply.SafeInvoke((Modes)mode, (long)tradeNum, (long)orderNum, classCode, secCode, price, balance, (int)volume, (Sides)direction);
		}

		#endregion

		protected override void DisposeManaged()
		{
			_api.Dispose();
			base.DisposeManaged();
		}
	}
}