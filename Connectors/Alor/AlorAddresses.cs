namespace StockSharp.Alor
{
	/// <summary>
	/// Адреса серверов системы Alor-Trade.
	/// </summary>
	public static class AlorAddresses
	{
		///// <summary>
		///// Порт сервера по умолчанию, равный 7800.
		///// </summary>
		//public const int DefaultPort = 7800;

		/// <summary>
		/// Демо сервер. IP адрес 213.181.12.15.
		/// </summary>
		public static readonly string Study = "213.181.12.15";

		/// <summary>
		/// Просмотровый сервер на Forts (фьючерсы).  адрес 212.5.162.212.
		/// </summary>
		public static readonly string RtsFuturesReadOnly = "212.5.162.212";

		/// <summary>
		/// Просмотровый сервер на Forts (опционы).  адрес 212.5.162.213.
		/// </summary>
		public static readonly string RtsOptionsReadOnly = "212.5.162.213";

		/// <summary>
		/// Просмотровый сервер на Rts Standard.  адрес 195.146.76.141.
		/// </summary>
		public static readonly string RtsStandardReadOnly = "195.146.76.141";

		/// <summary>
		/// Демо сервер «Срочный рынок FORTS».  адрес 213.181.16.52.
		/// </summary>
		public static readonly string FortsDemo = "213.181.16.52";

		/// <summary>
		/// Демо сервер «24/7». IP адрес train.alor.ru.
		/// </summary>
		public static readonly string H247Demo = "train.alor.ru";
	}
}