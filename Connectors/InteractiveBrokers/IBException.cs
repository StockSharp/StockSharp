namespace StockSharp.InteractiveBrokers
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Исключение, содержащее код и описание ошибки, полученной от Interactive Brokers.
	/// </summary>
	public class IBException : ApplicationException
	{
		internal IBException(string message)
			: base(message)
		{
		}

		internal IBException(int id, int code, string message)
			: base("{0} Номер {1} Код {2}".Put(message, id, code))
		{
			Id = id;
			ErrorCode = code;
		}

		/// <summary>
		/// Идентификатор ошибки.
		/// </summary>
		public int Id { get; private set; }

		/// <summary>
		/// Код ошибки.
		/// </summary>
		public int ErrorCode { get; private set; }
	}
}