namespace StockSharp.Algo.Testing
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// Состояния <see cref="IEmulationConnector"/>.
	/// </summary>
	public enum EmulationStates
	{
		/// <summary>
		/// Остановлен.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1128Key)]
		Stopped,

		/// <summary>
		/// Останавливается.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1114Key)]
		Stopping,

		/// <summary>
		/// Запускается.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1129Key)]
		Starting,

		/// <summary>
		/// Работает.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1130Key)]
		Started,

		/// <summary>
		/// В процессе приостановки.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1131Key)]
		Suspending, 

		/// <summary>
		/// Приостановлен.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1132Key)]
		Suspended,
	}
	
	/// <summary>
	/// Интерфейс подключения-эмулятора биржи.
	/// </summary>
	public interface IEmulationConnector : IConnector
	{
		/// <summary>
		/// Начать эмуляцию.
		/// </summary>
		/// <param name="startTime">Время в истории, с которого начать эмуляцию.</param>
		/// <param name="stopTime">Время в истории, на котором закончить эмуляцию.</param>
		void Start(DateTime startTime, DateTime stopTime);

		/// <summary>
		/// Остановить эмуляцию.
		/// </summary>
		void Stop();

		/// <summary>
		/// Приостановить эмуляцию.
		/// </summary>
		void Suspend();

		/// <summary>
		/// Возобновить эмуляцию.
		/// </summary>
		void Resume();

		/// <summary>
		/// Состояние эмулятора.
		/// </summary>
		EmulationStates State { get; }

		/// <summary>
		/// Событие о изменении состояния эмулятора <see cref="State"/>.
		/// </summary>
		event Action StateChanged;
	}
}