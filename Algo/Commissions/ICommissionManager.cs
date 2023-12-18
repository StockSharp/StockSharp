#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Commissions.Algo
File: ICommissionManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Commissions
{
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The commission calculating manager interface.
	/// </summary>
	public interface ICommissionManager : IPersistable
	{
		/// <summary>
		/// The list of commission calculating rules.
		/// </summary>
		ISynchronizedCollection<ICommissionRule> Rules { get; }

		/// <summary>
		/// Total commission.
		/// </summary>
		decimal Commission { get; }

		/// <summary>
		/// To reset the state.
		/// </summary>
		void Reset();

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
		decimal? Process(Message message);
	}
}