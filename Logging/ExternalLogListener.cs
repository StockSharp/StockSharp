#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: ExternalLogListener.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The logger sending messages to the external recipient <see cref="ILogListener"/>.
	/// </summary>
	public class ExternalLogListener : LogListener
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExternalLogListener"/>.
		/// </summary>
		/// <param name="logger">External recipient of messages.</param>
		public ExternalLogListener(ILogListener logger)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// External recipient of messages.
		/// </summary>
		public ILogListener Logger { get; }

		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			Logger.WriteMessages(messages);
		}
	}
}