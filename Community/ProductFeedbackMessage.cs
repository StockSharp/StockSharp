namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Product feedback message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ProductFeedbackMessage : BaseSubscriptionIdMessage<ProductFeedbackMessage>, ITransactionIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Product.
		/// </summary>
		[DataMember]
		public long ProductId { get; set; }

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
		public int Rating { get; set; }

		/// <summary>
		/// Rating.
		/// </summary>
		[DataMember]
		public DateTimeOffset CreationDate { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductFeedbackMessage"/>.
		/// </summary>
		public ProductFeedbackMessage()
			: base(CommunityMessageTypes.ProductFeedback)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => CommunityMessageTypes.ProductFeedbackType;

		/// <summary>
		/// Create a copy of <see cref="ProductFeedbackMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductFeedbackMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		public override void CopyTo(ProductFeedbackMessage destination)
		{
			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			base.CopyTo(destination);

			destination.TransactionId = TransactionId;
			destination.ProductId = ProductId;
			destination.Id = Id;
			destination.Text = Text;
			destination.Author = Author;
			destination.Rating = Rating;
			destination.Author = Author;
			destination.CreationDate = CreationDate;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",Product={ProductId}";

			if (TransactionId > 0)
				str += $",TrId={TransactionId}";

			if (Id != 0)
				str += $",Id={Id}";

			if (!Text.IsEmpty())
				str += $",Text={Text}";

			if (Author != 0)
				str += $",Author={Author}";

			if (Rating != 0)
				str += $",Rating={Rating}";

			if (CreationDate != default)
				str += $",Created={CreationDate}";

			return str;
		}
	}
}