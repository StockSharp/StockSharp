namespace StockSharp.Messages;

static partial class Extensions
{
	/// <summary>
	/// Determines the message contains any changes.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <returns>Check result.</returns>
	public static bool HasChanges<TMessage, TChange>(this BaseChangeMessage<TMessage, TChange> message)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
		=> message.CheckOnNull(nameof(message)).Changes.Count > 0;

	/// <summary>
	/// Try get change from message.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <returns>Change value.</returns>
	public static object TryGet<TMessage, TChange>(this TMessage message, TChange type)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		return message.Changes.TryGetValue(type);
	}

	/// <summary>
	/// Try get change from message.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <returns>Change value.</returns>
	public static decimal? TryGetDecimal<TMessage, TChange>(this TMessage message, TChange type)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		return (decimal?)message.TryGet(type);
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, object value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		message.Changes[type] = value;
		return message;
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, decimal value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		return message.Add(type, (object)value);
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, int value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		return message.Add(type, (object)value);
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, long value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		return message.Add(type, (object)value);
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, SecurityStates value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		message.Changes[type] = value;
		return message;
	}

	/// <summary>
	/// To add a change to the collection, if value is other than <see langword="null"/>.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, SecurityStates? value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == null)
			return message;

		return message.Add(type, value.Value);
	}

	/// <summary>
	/// To add a change to the collection, if value is other than <see langword="null"/>.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, string value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value.IsEmpty())
			return message;

		return message.Add(type, value);
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, Sides value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		message.Changes[type] = value;
		return message;
	}

	/// <summary>
	/// To add a change to the collection, if value is other than <see langword="null"/>.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, Sides? value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == null)
			return message;

		return message.Add(type, value.Value);
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, CurrencyTypes value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		message.Changes[type] = value;
		return message;
	}

	/// <summary>
	/// To add a change to the collection, if value is other than <see langword="null"/>.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, CurrencyTypes? value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == null)
			return message;

		return message.Add(type, value.Value);
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, PortfolioStates value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		message.Changes[type] = value;
		return message;
	}

	/// <summary>
	/// To add a change to the collection, if value is other than <see langword="null"/>.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, PortfolioStates? value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == null)
			return message;

		return message.Add(type, value.Value);
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, DateTimeOffset value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		message.Changes[type] = value;
		return message;
	}

	/// <summary>
	/// To add a change to the collection, if value is other than <see langword="null"/>.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, DateTimeOffset? value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == null)
			return message;

		return message.Add(type, value.Value);
	}

	/// <summary>
	/// Add change into collection.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage Add<TMessage, TChange>(this TMessage message, TChange type, bool value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		message.Changes[type] = value;
		return message;
	}

	/// <summary>
	/// To add a change to the collection, if value is other than <see langword="null"/>.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, bool? value)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == null)
			return message;

		return message.Add(type, value.Value);
	}

	/// <summary>
	/// To add a change to the collection, if value is other than 0.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <param name="isZeroAcceptable">Is zero value is acceptable values.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, decimal value, bool isZeroAcceptable = false)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == 0 && !isZeroAcceptable)
			return message;

		return message.Add(type, value);
	}

	/// <summary>
	/// To add a change to the collection, if value is other than 0 and <see langword="null" />.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <param name="isZeroAcceptable">Is zero value is acceptable values.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, decimal? value, bool isZeroAcceptable = false)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == null)
			return message;

		return message.TryAdd(type, value.Value, isZeroAcceptable);
	}

	/// <summary>
	/// To add a change to the collection, if value is other than 0.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <param name="isZeroAcceptable">Is zero value is acceptable values.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, int value, bool isZeroAcceptable = false)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == 0 && !isZeroAcceptable)
			return message;

		return message.Add(type, value);
	}

	/// <summary>
	/// To add a change to the collection, if value is other than 0 and <see langword="null" />.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <param name="isZeroAcceptable">Is zero value is acceptable values.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, int? value, bool isZeroAcceptable = false)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == null)
			return message;

		return message.TryAdd(type, value.Value, isZeroAcceptable);
	}

	/// <summary>
	/// To add a change to the collection, if value is other than 0.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <param name="isZeroAcceptable">Is zero value is acceptable values.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, long value, bool isZeroAcceptable = false)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == 0 && !isZeroAcceptable)
			return message;

		return message.Add(type, value);
	}

	/// <summary>
	/// To add a change to the collection, if value is other than 0 and <see langword="null" />.
	/// </summary>
	/// <typeparam name="TMessage">Change message type.</typeparam>
	/// <typeparam name="TChange">Change type.</typeparam>
	/// <param name="message">Change message.</param>
	/// <param name="type">Change type.</param>
	/// <param name="value">Change value.</param>
	/// <param name="isZeroAcceptable">Is zero value is acceptable values.</param>
	/// <returns>Change message.</returns>
	public static TMessage TryAdd<TMessage, TChange>(this TMessage message, TChange type, long? value, bool isZeroAcceptable = false)
		where TMessage : BaseChangeMessage<TMessage, TChange>, new()
	{
		if (value == null)
			return message;

		return message.TryAdd(type, value.Value, isZeroAcceptable);
	}
}