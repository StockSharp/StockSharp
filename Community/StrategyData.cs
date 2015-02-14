namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Данные о стратегии.
	/// </summary>
	[DataContract]
	public class StrategyData
	{
		/// <summary>
		/// Идентификатор.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Дата создания.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }

		/// <summary>
		/// Название.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Описание стратегии.
		/// </summary>
		[DataMember]
		public string Description { get; set; }

		/// <summary>
		/// Идентификатор топика на форуме, где идет обсуждение стратегии.
		/// </summary>
		[DataMember]
		public int TopicId { get; set; }

		/// <summary>
		/// Цена приобретения.
		/// </summary>
		[DataMember]
		public decimal Price { get; set; }

		/// <summary>
		/// Исходные коды (если стратегия распространяется в исходниках).
		/// </summary>
		[DataMember]
		public string SourceCode { get; set; }

		/// <summary>
		/// Скомпилированная сборка (если стратегия распространяется как готовая сборка).
		/// </summary>
		[DataMember]
		public byte[] CompiledAssembly { get; set; }

		/// <summary>
		/// Идентификатор автора.
		/// </summary>
		[DataMember]
		public long Author { get; set; }
	}
}