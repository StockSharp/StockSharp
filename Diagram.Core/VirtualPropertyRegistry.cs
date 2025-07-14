namespace StockSharp.Diagram;

/// <summary>
/// Extra properties registry.
/// </summary>
public static class VirtualPropertyRegistry
{
	private static readonly Dictionary<Type, Dictionary<string, (EntityProperty property, Type propType, Func<object, object> getter)>> _properties = [];
	private static readonly Dictionary<Type, Type> _interfaces = [];

	static VirtualPropertyRegistry()
	{
		AddProperty(new("SpreadMiddle", "SpreadMiddle", LocalizedStrings.SpreadMiddle, typeof(decimal?), null), (IOrderBookMessage b) => b.GetSpreadMiddle(default));

		AddProperty(new("BestBid", "BestBid", LocalizedStrings.BestBidDesc, typeof(QuoteChange?), null), (IOrderBookMessage b) => b.GetBestBid());

		AddProperty(new("BestAsk", "BestAsk", LocalizedStrings.BestAskDesc, typeof(QuoteChange?), null), (IOrderBookMessage b) => b.GetBestAsk());

		static void AddInterface<T1, T2>()
			where T1 : T2
			=> _interfaces.Add(typeof(T1), typeof(T2));

		AddInterface<QuoteChangeMessage, IOrderBookMessage>();
#pragma warning disable CS0618 // Type or member is obsolete
		AddInterface<MarketDepth, IOrderBookMessage>();
#pragma warning restore CS0618 // Type or member is obsolete
	}

	/// <summary>
	/// Add property.
	/// </summary>
	/// <typeparam name="TEntity">Entity type.</typeparam>
	/// <typeparam name="TValue">Property type.</typeparam>
	/// <param name="property">Property info.</param>
	/// <param name="getter">Getter.</param>
	public static void AddProperty<TEntity, TValue>(EntityProperty property, Func<TEntity, TValue> getter)
	{
		if (getter is null)
			throw new ArgumentNullException(nameof(getter));

		AddProperty(typeof(TEntity), property, typeof(TValue), o => getter((TEntity)o));
	}

	/// <summary>
	/// Add property.
	/// </summary>
	/// <param name="entityType">Entity type.</param>
	/// <param name="property">Property info.</param>
	/// <param name="propType">Property type</param>
	/// <param name="getter">Getter.</param>
	public static void AddProperty(Type entityType, EntityProperty property, Type propType, Func<object, object> getter)
	{
		if (entityType is null)	throw new ArgumentNullException(nameof(entityType));
		if (property is null)	throw new ArgumentNullException(nameof(property));
		if (propType is null)	throw new ArgumentNullException(nameof(propType));
		if (getter is null)		throw new ArgumentNullException(nameof(getter));

		propType = propType.GetUnderlyingType() ?? propType;

		_properties.SafeAdd(entityType).Add(property.Name, (property, propType, getter));
	}

	/// <summary>
	/// Get properties.
	/// </summary>
	/// <param name="entityType">Entity type.</param>
	/// <returns>Extra properties.</returns>
	public static IEnumerable<EntityProperty> GetVirtualProperties(this Type entityType)
	{
		if (_properties.TryGetValue(entityType, out var dict))
			return dict.Values.Select(t => t.property);

		return [];
	}

	/// <summary>
	/// Try get property type.
	/// </summary>
	/// <param name="entityType">Entity type.</param>
	/// <param name="propName">Property name.</param>
	/// <param name="propType">Property type</param>
	/// <returns>Operation result.</returns>
	public static bool TryGetVirtualPropertyType(this Type entityType, string propName, out Type propType)
	{
		if (_properties.TryGetValue(entityType, out var dict) && dict.TryGetValue(propName, out var t))
		{
			propType = t.propType;
			return true;
		}

		propType = null;
		return false;
	}

	/// <summary>
	/// Try get property value.
	/// </summary>
	/// <param name="entity">Entity.</param>
	/// <param name="propName">Property name.</param>
	/// <param name="value">Value.</param>
	/// <returns>Operation result.</returns>
	public static bool TryGetVirtualValue(this object entity, string propName, out object value)
	{
		if (entity is not null)
		{
			var entityType = entity.GetType();

			if (_interfaces.TryGetValue(entityType, out var i))
				entityType = i;

			if (_properties.TryGetValue(entityType, out var dict) && dict.TryGetValue(propName, out var t))
			{
				value = t.getter(entity);
				return true;
			}
		}

		value = null;
		return false;
	}
}