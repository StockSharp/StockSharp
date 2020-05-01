namespace StockSharp.Community.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Product feedback message.
	/// </summary>
	public class ProductFeedbackMessage : Message, IOriginalTransactionIdMessage
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Text.
		/// </summary>
		[DataMember]
		public string Text { get; set; }

		/// <summary>
		/// Text.
		/// </summary>
		[DataMember]
		public long Author { get; set; }

		/// <summary>
		/// Rating.
		/// </summary>
		[DataMember]
		public decimal Rating { get; set; }

		/// <summary>
		/// Rating.
		/// </summary>
		[DataMember]
		public DateTimeOffset CreationDate { get; set; }

		/// <inheritdoc />
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductFeedbackMessage"/>.
		/// </summary>
		public ProductFeedbackMessage()
			: base(CommunityMessageTypes.ProductFeedback)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="ProductFeedbackMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductFeedbackMessage
			{
				Id = Id,
				Text = Text,
				Author = Author,
				CreationDate = CreationDate,
				Rating = Rating,
				OriginalTransactionId = OriginalTransactionId,
			};
			CopyTo(clone);
			return clone;
		}
	}
}