namespace StockSharp.Community
{
	using System.Runtime.Serialization;

	/// <summary>
	/// Заявка на присоединение к чату.
	/// </summary>
	[DataContract]
	public class ChatJoin : ChatMessage
	{
		///// <summary>
		///// Является ли заявка запросом.
		///// </summary>
		//[DataMember]
		//public bool IsRequest { get; set; }
	}
}