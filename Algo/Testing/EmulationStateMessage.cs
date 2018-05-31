#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: EmulationStateMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// The message, informing about the emulator state change.
	/// </summary>
	public class EmulationStateMessage : Message
	{
		/// <summary>
		/// Date in history for starting the paper trading.
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Date in history to stop the paper trading (date is included).
		/// </summary>
		public DateTimeOffset StopDate { get; set; }

		/// <summary>
		/// The state been transferred.
		/// </summary>
		public EmulationStates State { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EmulationStateMessage"/>.
		/// </summary>
		public EmulationStateMessage()
			: base(ExtendedMessageTypes.EmulationState)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="EmulationStateMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new EmulationStateMessage
			{
				State = State,
				StartDate = StartDate,
				StopDate = StopDate,
			};
		}
	}
}