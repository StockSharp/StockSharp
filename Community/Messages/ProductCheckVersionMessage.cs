namespace StockSharp.Community.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Product check version message.
	/// </summary>
	public class ProductCheckVersionMessage : Message, ITransactionIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Product ID.
		/// </summary>
		[DataMember]
		public long ProductId { get; set; }

		/// <summary>
		/// Local files info (name and hash).
		/// </summary>
		[DataMember]
		public Tuple<string, string>[] LocalFiles { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductCheckVersionMessage"/>.
		/// </summary>
		public ProductCheckVersionMessage()
			: base(CommunityMessageTypes.ProductCheckVersion)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="ProductCheckVersionMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductCheckVersionMessage
			{
				TransactionId = TransactionId,
				ProductId = ProductId,
				LocalFiles = LocalFiles?.Select(t => Tuple.Create(t.Item1, t.Item2)).ToArray(),
			};
			CopyTo(clone);
			return clone;
		}
	}
}