#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: ConnectMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	using System.Net;

	using StockSharp.Transaq.Native;

	internal class ConnectMessage : BaseCommandMessage
	{
		public ConnectMessage() : base(ApiCommands.Connect)
		{
		}

		public string Login { get; set; }
		public string Password { get; set; }
		public EndPoint EndPoint { get; set; }
		public string LogsDir { get; set; }
		public ApiLogLevels? LogLevel { get; set; }
		public bool Autopos { get; set; }
		public string NotesFile { get; set; }
		public Proxy Proxy { get; set; }
		public int? RqDelay { get; set; }
		public int? SessionTimeout { get; set; }
		public int? RequestTimeout { get; set; }
		public bool MicexRegisters { get; set; }
		public bool Milliseconds { get; set; }
		public bool Utc { get; set; }
	}
}