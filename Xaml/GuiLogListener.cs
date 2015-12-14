#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: GuiLogListener.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;

	using Ecng.Xaml;

	using StockSharp.Logging;

	/// <summary>
	/// The logger recording data to visual components (for example, <see cref="Monitor"/> or <see cref="LogControl"/>) that require synchronization with the GUI threads when new messages are recorded <see cref="LogMessage"/>.
	/// </summary>
	public class GuiLogListener : LogListener
	{
		private readonly GuiDispatcher _dispatcher = GuiDispatcher.GlobalDispatcher;
		private readonly ILogListener _listener;

		/// <summary>
		/// Initializes a new instance of the <see cref="GuiLogListener"/>.
		/// </summary>
		/// <param name="listener">The visual component that requires synchronization with GUI threads when new messages are recorded <see cref="LogMessage"/>.</param>
		public GuiLogListener(ILogListener listener)
		{
			if (listener == null)
				throw new ArgumentNullException(nameof(listener));

			_listener = listener;
		}

		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			_dispatcher.AddAction(() => _listener.WriteMessages(messages));
		}
	}
}