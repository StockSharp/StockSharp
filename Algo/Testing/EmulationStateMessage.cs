namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

	/// <summary>
	/// Сообщение, информирующее об изменении состояния эмулятора.
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
		/// Передаваемое состояние.
		/// </summary>
		public EmulationStates State { get; set; }

		/// <summary>
		/// Создать <see cref="EmulationStateMessage"/>.
		/// </summary>
		public EmulationStateMessage()
			: base(ExtendedMessageTypes.EmulationState)
		{
		}

		/// <summary>
		/// Создать копию <see cref="Message"/>.
		/// </summary>
		/// <returns>Копия.</returns>
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