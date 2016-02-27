#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.Native.ETrade
File: ETradeResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade.Native
{
	using System;

	abstract class ETradeResponse
	{
		public ETradeRequest Request {get; private set;}
		public Exception Exception {get; private set;}

		protected ETradeResponse(ETradeRequest req) { Request = req; } 

		public abstract void Process();

		public static ETradeResponse<TResp> Create<TResp>(ETradeRequest<TResp> req, TResp responseData, Exception exception)
		{
			return new ETradeResponse<TResp>(req, responseData) {Exception = exception};
		}
	}

	class ETradeResponse<TResp> : ETradeResponse
	{
		public new ETradeRequest<TResp> Request => (ETradeRequest<TResp>)base.Request;
		public TResp Data {get; private set;}

		public ETradeResponse(ETradeRequest<TResp> req, TResp responseData) : base(req)
		{
			Data = responseData;
		}

		public override void Process()
		{
			if(Request.ResponseHandler == null) return;

			Request.ResponseHandler(this);
		}
	}
}