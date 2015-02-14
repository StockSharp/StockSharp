namespace StockSharp.Algo
{
	using System;
	using System.Data;
	using System.ServiceModel;

	/// <summary>
	/// Интерфейс для доступа к сервису валют.
	/// </summary>
	[ServiceContract(Namespace = "http://web.cbr.ru/")]
	public interface IDailyInfoSoap
	{
		/// <summary>
		/// Получить курсы валют на определенную дату.
		/// </summary>
		/// <param name="date">Дата курсов.</param>
		/// <returns>Курсы валют.</returns>
		[OperationContract(Action = "http://web.cbr.ru/GetCursOnDate", ReplyAction = "*")]
		[XmlSerializerFormat]
		DataSet GetCursOnDate([MessageParameter(Name = "On_date")]DateTime date);
	}
}