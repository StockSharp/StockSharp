#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.CQG.CQG
File: CQGMessageAdapter_Transaction.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.CQG
{
	using global::CQG;

	/// <summary>
	/// CQG message adapter.
	/// </summary>
	partial class CQGMessageAdapter
	{
		private void SessionOnPositionsStatementResolved(CQGPositionsStatement cqgPositionsStatement, CQGError cqgError)
		{

		}

		private void SessionOnOrderChanged(eChangeType changeType, CQGOrder cqgOrder, CQGOrderProperties oldProperties, CQGFill cqgFill, CQGError cqgError)
		{

		}

		private void SessionOnAlgorithmicOrderRegistrationComplete(string guid, CQGError cqgError)
		{

		}

		private void SessionOnAlgorithmicOrderPlaced(string guid, CQGAlgorithmicOrderParameters mainParams, CQGAlgorithmicOrderProperties customProps)
		{

		}

		private void SessionOnAccountChanged(eAccountChangeType changeType, CQGAccount cqgAccount, CQGPosition cqgPosition)
		{

		}
	}
}