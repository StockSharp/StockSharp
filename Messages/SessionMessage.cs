namespace StockSharp.Messages
{
	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Состояния торговой сессии.
	/// </summary>
	public enum SessionStates
	{
		/// <summary>
		/// Сессия назначена. Нельзя ставить заявки, но можно удалять.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str399Key)]
		Assigned,

		/// <summary>
		/// Сессия идет. Можно ставить и удалять заявки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str238Key)]
		Active,

		/// <summary>
		/// Приостановка торгов по всем инструментам. Нельзя ставить заявки, но можно удалять.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str400Key)]
		Paused,

		/// <summary>
		/// Сессия принудительно завершена. Нельзя ставить и удалять заявки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str401Key)]
		ForceStopped,

		/// <summary>
		/// Сессия завершена по времени. Нельзя ставить и удалять заявки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str402Key)]
		Ended,
	}

	/// <summary>
	/// Сообщение о изменении состояния сессии.
	/// </summary>
	public class SessionMessage : Message
	{
		/// <summary>
		/// Создать <see cref="SessionMessage"/>.
		/// </summary>
		public SessionMessage()
			: base(MessageTypes.Session)
		{
		}

		/// <summary>
		/// Код площадки.
		/// </summary>
		public string BoardCode { get; set; }

		/// <summary>
		/// Состояние торговой сессии.
		/// </summary>
		public SessionStates State { get; set; }

		/// <summary>
		/// Создать копию объекта <see cref="SessionMessage"/>.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Message Clone()
		{
			return new SessionMessage
			{
				BoardCode = BoardCode,
				State = State,
				LocalTime = LocalTime
			};
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Board={0},State={1}".Put(BoardCode, State);
		}
	}
}