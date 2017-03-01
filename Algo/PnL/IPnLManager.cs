#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.PnL.Algo
File: IPnLManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.PnL
{
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The interface of the profit-loss calculation manager.
	/// </summary>
	public interface IPnLManager : IPersistable
	{
		/// <summary>
		/// Total profit-loss.
		/// </summary>
		decimal PnL { get; }

		/// <summary>
		/// The value of realized profit-loss.
		/// </summary>
		decimal RealizedPnL { get; }

		/// <summary>
		/// The value of unrealized profit-loss.
		/// </summary>
		decimal? UnrealizedPnL { get; }

		/// <summary>
		/// To zero <see cref="PnL"/>.
		/// </summary>
		void Reset();

		/// <summary>
		/// To process the message, containing market data or trade. If the trade was already processed earlier, previous information returns.
		/// </summary>
		/// <param name="message">The message, containing market data or trade.</param>
		/// <returns>Information on new trade.</returns>
		PnLInfo ProcessMessage(Message message);
	}
}