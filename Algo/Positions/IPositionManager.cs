#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Positions.Algo
File: IPositionManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Positions
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	/// <summary>
	/// The interface for the position calculation manager.
	/// </summary>
	public interface IPositionManager
	{
		/// <summary>
		/// The position aggregate value.
		/// </summary>
		decimal Position { get; set; }

		/// <summary>
		/// The security for which <see cref="Position"/> will be calculated.
		/// </summary>
		SecurityId? SecurityId { get; set; }

		/// <summary>
		/// Positions, grouped by instruments and portfolios.
		/// </summary>
		IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>> Positions { get; set; }

		/// <summary>
		/// The event of new position occurrence in <see cref="Positions"/>.
		/// </summary>
		event Action<Tuple<SecurityId, string>, decimal> NewPosition;

		/// <summary>
		/// The event of position change in <see cref="Positions"/>.
		/// </summary>
		event Action<Tuple<SecurityId, string>, decimal> PositionChanged;

		/// <summary>
		/// To null position.
		/// </summary>
		void Reset();

		/// <summary>
		/// To calculate position.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The position by order or trade.</returns>
		decimal? ProcessMessage(Message message);
	}
}