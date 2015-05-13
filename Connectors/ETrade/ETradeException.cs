namespace StockSharp.ETrade
{
	using System;

	class ETradeException : ApplicationException
	{
		internal ETradeException(string msg, Exception inner = null)
			: base(msg, inner)
		{
		}
	}

	class ETradeOrderFailException : ETradeException
	{
		internal ETradeOrderFailException(string msg, Exception inner = null)
			: base(msg, inner)
		{
		}
	}

	class ETradeAuthorizationRenewFailed : ETradeException
	{
		internal ETradeAuthorizationRenewFailed(string msg = "Error updating access token.", Exception inner = null)
			: base(msg, inner)
		{
		}
	}

	class ETradeAuthorizationFailedException : ETradeException
	{
		internal ETradeAuthorizationFailedException(string msg = "Not authorized.", Exception inner = null)
			: base(msg, inner)
		{
		}
	}

	class ETradeUnauthorizedException : ETradeException
	{
		internal ETradeUnauthorizedException(string msg = "Received code 'Unauthorized' from the server.", Exception inner = null)
			: base(msg, inner)
		{
		}
	}

	class ETradeConnectionFailedException : ETradeException
	{
		internal ETradeConnectionFailedException(string msg = "Connection failed.", Exception inner = null)
			: base(msg, inner)
		{
		}
	}
}