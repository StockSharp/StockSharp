#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Commissions.Algo
File: ICommissionRule.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Commissions
{
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The commission calculating rule interface.
	/// </summary>
	public interface ICommissionRule : IPersistable
	{
		/// <summary>
		/// Header.
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Total commission.
		/// </summary>
		decimal Commission { get; }

		/// <summary>
		/// Commission value.
		/// </summary>
		Unit Value { get; }

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