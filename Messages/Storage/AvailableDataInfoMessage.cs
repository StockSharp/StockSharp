namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// Available data info message.
	/// </summary>
	public class AvailableDataInfoMessage : BaseSubscriptionIdMessage<AvailableDataInfoMessage>, ISecurityIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AvailableDataInfoMessage"/>.
		/// </summary>
		public AvailableDataInfoMessage()
			: base(MessageTypes.AvailableDataInfo)
		{
		}

		/// <inheritdoc />
		[DataMember]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Market data type.
		/// </summary>
		[DataMember]
		public DataType FileDataType { get; set; }

		private DateTime[] _dates = Array.Empty<DateTime>();

		/// <summary>
		/// Dates.
		/// </summary>
		[DataMember]
		public DateTime[] Dates
		{
			get => _dates;
			set => _dates = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Storage format.
		/// </summary>
		[DataMember]
		public int Format { get; set; }

		/// <inheritdoc />
		public override DataType DataType => DataType.Create<AvailableDataInfoMessage>();

		/// <summary>
		/// Create a copy of <see cref="AvailableDataInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new AvailableDataInfoMessage
			{
				SecurityId = SecurityId,
				FileDataType = FileDataType?.TypedClone(),
				Dates = Dates,
				Format = Format,
			};

			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",SecId={SecurityId},DT={FileDataType},DatesLen={Dates.Length},Fmt={Format}";
		}
	}
}