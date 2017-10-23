namespace StockSharp.Messages
{
	using System;
	using System.Data;
	using System.ServiceModel;

	/// <summary>
	/// The interface for access to currency service.
	/// </summary>
	[ServiceContract(Namespace = "http://web.cbr.ru/")]
	public interface IDailyInfoSoap
	{
		/// <summary>
		/// To get currency exchange rates for the specific date.
		/// </summary>
		/// <param name="date">Date of rates.</param>
		/// <returns>Currency exchange rates.</returns>
		[OperationContract(Action = "http://web.cbr.ru/GetCursOnDate", ReplyAction = "*")]
		[XmlSerializerFormat]
		DataSet GetCursOnDate([MessageParameter(Name = "On_date")]DateTime date);
	}
}