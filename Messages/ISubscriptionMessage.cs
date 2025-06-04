namespace StockSharp.Messages;

/// <summary>
/// Fill gaps days.
/// </summary>
public enum FillGapsDays
{
	/// <summary>
	/// Weekdays.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.WeekdaysKey,
		Description = LocalizedStrings.WeekdaysDescKey)]
	Weekdays,

	/// <summary>
	/// All days.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.AllKey,
		Description = LocalizedStrings.AllDaysKey)]
	All,
}

/// <summary>
/// The interface describing an message with <see cref="IsSubscribe"/> property.
/// </summary>
public interface ISubscriptionMessage : ITransactionIdMessage, IOriginalTransactionIdMessage, IMessage
{
	/// <summary>
	/// Message contains fields with non default values.
	/// </summary>
	bool FilterEnabled { get; }

	/// <summary>
	/// Determine whether the message is a request for a specific item (e.g. order, position, etc.) or not.
	/// </summary>
	bool SpecificItemRequest { get; }

	/// <summary>
	/// Start date, from which data needs to be retrieved.
	/// </summary>
	DateTimeOffset? From { get; set; }

	/// <summary>
	/// End date, until which data needs to be retrieved.
	/// </summary>
	DateTimeOffset? To { get; set; }

	/// <summary>
	/// The message is subscription.
	/// </summary>
	bool IsSubscribe { get; set; }

	/// <summary>
	/// Skip count.
	/// </summary>
	long? Skip { get; set; }

	/// <summary>
	/// Max count.
	/// </summary>
	long? Count { get; set; }

	/// <summary>
	/// Data type info.
	/// </summary>
	DataType DataType { get; }

	/// <summary>
	/// <see cref="FillGapsDays"/>.
	/// </summary>
	FillGapsDays? FillGaps { get; set; }
}