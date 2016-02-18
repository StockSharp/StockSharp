using System;

namespace StockSharp.Studio.Core.Commands {
	public class ErrorCommand : BaseStudioCommand {
		public Exception Error {get;}
		public string Message {get;}

		public ErrorCommand(Exception error)
		{
			if(error == null)
				throw new ArgumentNullException(nameof(error));

			Error = error;
			Message = error.Message;
		}

		public ErrorCommand(string message)
		{
			Message = message;
		}

		public override string ToString()
		{
			return Error?.ToString() ?? Message;
		}
	}
}
