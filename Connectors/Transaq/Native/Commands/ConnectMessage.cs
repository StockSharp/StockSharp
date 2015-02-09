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
		public ApiLogLevels LogLevel { get; set; }
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