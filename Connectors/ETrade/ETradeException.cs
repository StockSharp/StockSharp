#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.ETrade
File: ETradeException.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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