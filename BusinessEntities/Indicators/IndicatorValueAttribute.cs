namespace StockSharp.Algo.Indicators;

/// <summary>
/// Attribute, applied to indicator, to provide information about type of values <see cref="IIndicatorValue"/>.
/// </summary>
public abstract class IndicatorValueAttribute : Attribute
{
	/// <summary>
	/// Value type.
	/// </summary>
	public Type Type { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorValueAttribute"/>.
	/// </summary>
	/// <param name="type">Value type.</param>
	protected IndicatorValueAttribute(Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		if (!type.Is<IIndicatorValue>())
			throw new ArgumentException(LocalizedStrings.TypeNotImplemented.Put(type.Name, nameof(IIndicatorValue)), nameof(type));

		Type = type;
	}
}

/// <summary>
/// Attribute, applied to indicator, to provide information about type of input values <see cref="IIndicatorValue"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IndicatorInAttribute"/>.
/// </remarks>
/// <param name="type">Values type.</param>
[AttributeUsage(AttributeTargets.Class)]
public class IndicatorInAttribute(Type type) : IndicatorValueAttribute(type)
{
}

/// <summary>
/// Attribute, applied to indicator, to provide information about type of output values <see cref="IIndicatorValue"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IndicatorOutAttribute"/>.
/// </remarks>
/// <param name="type">Values type.</param>
[AttributeUsage(AttributeTargets.Class)]
public class IndicatorOutAttribute(Type type) : IndicatorValueAttribute(type)
{
}

/// <summary>
/// Attribute, applied to indicator that must be hidden from any UI selections.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IndicatorHiddenAttribute : Attribute
{
}