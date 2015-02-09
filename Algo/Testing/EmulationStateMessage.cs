namespace StockSharp.Algo.Testing
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// Сообщение, информирующее об изменении состояния эмулятора.
	/// </summary>
	class EmulationStateMessage : Message
	{
		/// <summary>
		/// Дата в истории, с которой необходимо начать эмуляцию.
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Дата в истории, на которой необходимо закончить эмуляцию (дата включается).
		/// </summary>
		public DateTimeOffset StopDate { get; set; }

		///// <summary>
		///// Предыдущее состояние.
		///// </summary>
		//public EmulationStates OldState { get; set; }

		/// <summary>
		/// Текущее состояние.
		/// </summary>
		public EmulationStates NewState { get; set; }

		/// <summary>
		/// Создать <see cref="EmulationStateMessage"/>.
		/// </summary>
		public EmulationStateMessage()
			: base(ExtendedMessageTypes.EmulationState)
		{
		}

		/// <summary>
		/// Создать копию объекта <see cref="Message"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new EmulationStateMessage
			{
				//OldState = OldState,
				NewState = NewState,
				StartDate = StartDate,
				StopDate = StopDate,
			};
		}
	}
}