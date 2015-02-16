namespace StockSharp.Quik.Native
{
	/// <summary>
	/// Системные коды (успешные и ошибочные) библиотеки TRANS2QUIK.dll, которые возвращает Quik.
	/// </summary>
	public enum Codes
	{
		/// <summary>
		/// Метод вызван успешно.
		/// </summary>
		Success = 0,

		/// <summary>
		/// Произошла ошибка.
		/// </summary>
		Failed = 1,

		/// <summary>
		/// В указанном каталоге либо отсутствует INFO.EXE,
		/// либо у него не запущен сервис обработки внешних подключений.
		/// </summary>
		QuikTerminalNotFound = 2,

		/// <summary>
		/// Используемая версия Trans2QUIK.DLL указанным INFO.EXE не поддерживается.
		/// </summary>
		DllVersionNotSupported = 3,

		/// <summary>
		/// Соединение уже установлено.
		/// </summary>
		AlreadyConnectedToQuik = 4,

		/// <summary>
		/// Неправильный синтаксис.
		/// </summary>
		WrongSyntax = 5,

		/// <summary>
		/// Подключение с сервером не установлено.
		/// </summary>
		QuikNotConnected = 6,

		/// <summary>
		/// Подключение с терминалом не установлено.
		/// </summary>
		DllNotConnected = 7,

		/// <summary>
		/// Подключение с Quik установлено.
		/// </summary>
		QuikConnected = 8,

		/// <summary>
		/// Подключение с Quik разорвано.
		/// </summary>
		QuikDisconnected = 9,

		/// <summary>
		/// Подключение с терминалом установлено.
		/// </summary>
		DllConnected = 10,

		/// <summary>
		/// Подключение с терминалом разорвано.
		/// </summary>
		DllDisconnected = 11,

		/// <summary>
		/// Ошибка выделения памяти.
		/// </summary>
		MemoryAllocationError = 12,

		/// <summary>
		/// Неправильный идентификатор соединения.
		/// </summary>
		WrongConnectionHandle = 13,

		/// <summary>
		/// Неправильный набор параметров.
		/// </summary>
		WrongInputParams = 14,
	}
}