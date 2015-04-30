namespace StockSharp.ETrade.Native
{
	using System;

	abstract class ETradeResponse
	{
		public ETradeRequest Request {get; private set;}
		public Exception Exception {get; private set;}

		protected ETradeResponse(ETradeRequest req) { Request = req; } 

		abstract public void Process();

		public static ETradeResponse<TResp> Create<TResp>(ETradeRequest<TResp> req, TResp responseData, Exception exception)
		{
			return new ETradeResponse<TResp>(req, responseData) {Exception = exception};
		}
	}

	class ETradeResponse<TResp> : ETradeResponse
	{
		new public ETradeRequest<TResp> Request {get {return (ETradeRequest<TResp>)base.Request; }}
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