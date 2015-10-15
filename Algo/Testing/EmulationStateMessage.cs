namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

	/// <summary>
	/// The message, informing about the emulator state change.
	/// </summary>
	class EmulationStateMessage : Message
	{
		///// <summary>
		///// Дата в истории, с которой необходимо начать эмуляцию.
		///// </summary>
		//public DateTimeOffset StartDate { get; set; }

		///// <summary>
		///// Дата в истории, на которой необходимо закончить эмуляцию (дата включается).
		///// </summary>
		//public DateTimeOffset StopDate { get; set; }

		///// <summary>
		///// Предыдущее состояние.
		///// </summary>
		//public EmulationStates OldState { get; set; }

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
		/// Create a copy of <see cref="Message"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new EmulationStateMessage
			{
				//OldState = OldState,
				State = State,
				//StartDate = StartDate,
				//StopDate = StopDate,
			};
		}
	}
}