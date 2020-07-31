namespace StockSharp.Messages
{
	/// <summary>
	/// Process suspended action.
	/// </summary>
	public class ProcessSuspendedMessage : Message
	{
		/// <summary>
		/// Additional argument.
		/// </summary>
		public object Arg { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProcessSuspendedMessage"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="arg">Additional argument.</param>
		public ProcessSuspendedMessage(IMessageAdapter adapter, object arg = default)
			: base(MessageTypes.ProcessSuspended)
		{
			this.LoopBack(adapter);
			Arg = arg;
		}

		/// <summary>
		/// Create a copy of <see cref="ProcessSuspendedMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone() => new ProcessSuspendedMessage(Adapter, Arg);
	}
}