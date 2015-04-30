namespace StockSharp.ETrade.Native
{
	using System;
	using StockSharp.Logging;

	abstract class ETradeRequest
	{
		public bool IsDone {get; protected set;}
		public virtual bool IsRequestRateLimited {get { return true; }}

		public ETradeResponse ExecuteNextPart(ETradeClient client)
		{
			IsDone = false;
			var response = ExecuteNextPartInternal(client.Api);
			IsDone = GetIsDone() || response.Exception != null;

			if (response.Exception != null)
			{
				client.AddWarningLog("Request '{0}' completed with exception: {1}", GetType().Name, response.Exception);
			}

			return response;
		}

		protected virtual bool GetIsDone() { return true; }
		protected abstract ETradeResponse ExecuteNextPartInternal(ETradeApi api);

		protected TRet PerformApiRequest<TRet>(Func<TRet> requestAction, out Exception exception)
		{
			exception = null;
			var response = default(TRet);

			try
			{
				response = requestAction();
			}
			catch (Exception e)
			{
				exception = e;
			}

			return response;
		}
	}

	abstract class ETradeRequest<TResp> : ETradeRequest
	{
		public Action<ETradeResponse<TResp>> ResponseHandler { get; set; }
	}

	class ETradeRateLimitsRequest : ETradeRequest<RateLimitStatus>
	{
		readonly string _moduleName;

		public ETradeRateLimitsRequest(string moduleName) { _moduleName = moduleName; }

		public override bool IsRequestRateLimited {get { return false; }}

		protected override ETradeResponse ExecuteNextPartInternal(ETradeApi api)
		{
			Exception ex;

			var result = PerformApiRequest(() => api.GetRateLimitStatus(_moduleName), out ex);

			return ETradeResponse.Create(this, result, ex);
		}
	}
}