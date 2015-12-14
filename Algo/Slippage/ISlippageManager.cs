#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Slippage.Algo
File: ISlippageManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Slippage
{
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The interface for the slippage calculation manager.
	/// </summary>
	public interface ISlippageManager : IPersistable
	{
		/// <summary>
		/// Total slippage.
		/// </summary>
		decimal Slippage { get; }

		/// <summary>
		/// To reset the state.
		/// </summary>
		void Reset();

		/// <summary>
		/// To calculate slippage.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The slippage. If it is impossible to calculate slippage, <see langword="null" /> will be returned.</returns>
		decimal? ProcessMessage(Message message);
	}
}