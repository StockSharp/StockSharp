namespace StockSharp.Algo.Equity
{
	using System;
	using System.Runtime.Serialization;

	///<summary>
	/// Данные по эквити.
	///</summary>
	[DataContract]
	[Serializable]
	public class EquityData
	{
		///<summary>
		/// Отметка времени, в которое значение эквити было равным <see cref="Value"/>.
		///</summary>
		[DataMember]
		public DateTime Time { get; set; }

		///<summary>
		/// Значение эквити.
		///</summary>
		[DataMember]
		public decimal Value { get; set; }
	}
}