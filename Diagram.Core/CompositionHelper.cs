namespace StockSharp.Diagram;

using Ecng.Reflection;

/// <summary>
/// Helpers.
/// </summary>
public static class CompositionHelper
{
	/// <summary>
	/// Fill <see cref="ICompositionRegistry.DiagramElements"/> by <see cref="GetDiagramElements"/>.
	/// </summary>
	/// <param name="registry"><see cref="ICompositionRegistry"/>.</param>
	public static void FillDefault(this ICompositionRegistry registry)
	{
		if (registry is null)
			throw new ArgumentNullException(nameof(registry));

		registry.DiagramElements.AddRange(GetDiagramElements());
	}

	private static void EnsureDiagramElements()
	{
		if (_diagramElements is not null)
			return;

		_diagramElements = [.. typeof(DiagramElement).Assembly.GetTypes()
			.Where(t =>
				t.IsRequiredType<DiagramElement>() &&
				t.IsBrowsable() &&
				t != typeof(CompositionDiagramElement)
			)
			.OrderBy(t => t.Name)];
	}

	/// <summary>
	/// Add <see cref="DiagramElement"/> type.
	/// </summary>
	/// <typeparam name="T"><see cref="DiagramElement"/> type.</typeparam>
	public static void AddDiagramElement<T>()
		where T : DiagramElement
	{
		AddDiagramElement(typeof(T));
	}

	/// <summary>
	/// Add <see cref="DiagramElement"/> type.
	/// </summary>
	/// <param name="elemType"><see cref="DiagramElement"/> type.</param>
	public static void AddDiagramElement(Type elemType)
	{
		if (elemType is null)
			throw new ArgumentNullException(nameof(elemType));
	
		if (!elemType.Is<DiagramElement>())
			throw new ArgumentException(LocalizedStrings.TypeNotImplemented.Put(elemType.Name, nameof(DiagramElement)), nameof(elemType));

		EnsureDiagramElements();

		_diagramElements = [.. _diagramElements, .. new[] { elemType }];
	}

	private static Type[] _diagramElements;

	/// <summary>
	/// Get all diagram elements.
	/// </summary>
	/// <returns>All diagram elements.</returns>
	public static IEnumerable<DiagramElement> GetDiagramElements()
	{
		EnsureDiagramElements();

		return [.. _diagramElements.Select(t => t.CreateInstance<DiagramElement>())];
	}

	/// <summary>
	/// To continue and stop at the next element.
	/// </summary>
	/// <param name="syncObject"><see cref="DebuggerSyncObject"/></param>
	public static void ContinueAndWaitOnNext(this DebuggerSyncObject syncObject)
	{
		if (syncObject is null)
			throw new ArgumentNullException(nameof(syncObject));

		syncObject.SetWaitOnNext();
		syncObject.Continue();
	}

	private static void CheckForUnitValues(ref IComparable left, ref IComparable right)
	{
		var leftUnit = left as Unit;
		var rightUnit = right as Unit;

		if (leftUnit != null)
		{
			if (rightUnit == null && right.GetType().IsNumeric())
				right = new Unit(right.To<decimal>());
		}
		else
		{
			if (rightUnit != null && left.GetType().IsNumeric())
				left = new Unit(left.To<decimal>());
		}
	}

	private static void CheckForIndicatorValues(ref IComparable left, ref IComparable right)
	{
		var leftIndicator = left as IIndicatorValue;
		var rightIndicator = right as IIndicatorValue;

		if (leftIndicator != null && rightIndicator != null)
			return;

		try
		{
			if (leftIndicator != null)
			{
				var leftDecimal = leftIndicator.ToDecimal();
				var type = right.GetType();

				if (type.IsNumeric())
				{
					left = leftDecimal;
					right = right.To<decimal>();
				}
				else if (type == typeof(Unit))
				{
					left = new Unit(leftDecimal);
				}
			}

			if (rightIndicator != null)
			{
				var rightDecimal = rightIndicator.ToDecimal();
				var type = left.GetType();

				if (type.IsNumeric())
				{
					left = left.To<decimal>();
					right = rightDecimal;
				}
				else if (type == typeof(Unit))
				{
					right = new Unit(rightDecimal);
				}
			}
		}
		catch
		{
		}
	}

	internal static void CheckForValueTypes(ref IComparable left, ref IComparable right)
	{
		CheckForUnitValues(ref left, ref right);
		CheckForIndicatorValues(ref left, ref right);
	}

	internal static DiagramSocket AllowConvertToNumeric(this DiagramSocket socket)
	{
		socket.AvailableTypes.Clear();
		socket.AvailableTypes.Add(DiagramSocketType.Candle);
		socket.AvailableTypes.Add(DiagramSocketType.IndicatorValue);
		socket.AvailableTypes.Add(DiagramSocketType.Unit);
		socket.AvailableTypes.Add(DiagramSocketType.Quote);
		socket.AvailableTypes.Add(DiagramSocketType.MarketDepth);

		return socket;
	}

	internal static DiagramSocket ResetAvailableTypes(this DiagramSocket socket)
	{
		socket.AvailableTypes.Clear();
		socket.AvailableTypes.Add(DiagramSocketType.Any);

		return socket;
	}

	private static readonly HashSet<DiagramSocketType> _editable = new(
	[
		DiagramSocketType.Any, DiagramSocketType.Bool, DiagramSocketType.Unit,
		DiagramSocketType.Date, DiagramSocketType.Time,
		DiagramSocketType.Side, DiagramSocketType.CandleStates, DiagramSocketType.OrderState,
		DiagramSocketType.Security, DiagramSocketType.Portfolio, DiagramSocketType.String,
	]);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static bool IsEditable(this DiagramSocketType type) => _editable.Contains(type);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="sockets"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public static DiagramSocket FindById(this IEnumerable<DiagramSocket> sockets, string id)
	{
		return sockets.FirstOrDefault(s => s.Id.EqualsIgnoreCase(id));
	}

	internal static void SafeGetValue<T>(this SettingsStorage storage, string key, Action<T> action, bool processNull = false)
		where T : class
	{
		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		if (key == null)
			throw new ArgumentNullException(nameof(key));

		if (action == null)
			throw new ArgumentNullException(nameof(action));

		var value = storage.GetValue<T>(key);

		if (value == null && !processNull)
			return;

		try
		{
			action(value);
		}
		catch (Exception ex)
		{
			ex.LogError();
		}
	}

	/// <summary>
	/// Get From socket for the specified link.
	/// </summary>
	/// <param name="link"><see cref="ICompositionModelLink"/></param>
	/// <param name="behavior"><see cref="ICompositionModelBehavior{TNode, TLink}"/></param>
	/// <returns><see cref="DiagramSocket"/></returns>
	public static DiagramSocket GetFromSocket<TNode, TLink>(this ICompositionModelLink link, ICompositionModelBehavior<TNode, TLink> behavior)
		where TNode : ICompositionModelNode
		where TLink : ICompositionModelLink
		=> link.CheckOnNull(nameof(link)).IsConnected ? behavior.FindNodeByKey(link.From)?.Element?.OutputSockets?.FindById(link.FromPort) : null;

	/// <summary>
	/// Get To socket for the specified link.
	/// </summary>
	/// <param name="link"><see cref="ICompositionModelLink"/></param>
	/// <param name="behavior"><see cref="ICompositionModelBehavior{TNode, TLink}"/></param>
	/// <returns><see cref="DiagramSocket"/></returns>
	public static DiagramSocket GetToSocket<TNode, TLink>(this ICompositionModelLink link, ICompositionModelBehavior<TNode, TLink> behavior)
		where TNode : ICompositionModelNode
		where TLink : ICompositionModelLink
		=> link.CheckOnNull(nameof(link)).IsConnected ? behavior.FindNodeByKey(link.To)?.Element?.InputSockets?.FindById(link.ToPort) : null;

	/// <summary>
	/// Check the specified <see cref="DiagramStrategy"/> contains code element.
	/// </summary>
	/// <param name="strategy"><see cref="DiagramStrategy"/></param>
	/// <returns>Check result.</returns>
	public static bool HasCode(this DiagramStrategy strategy)
		=> strategy.CheckOnNull(nameof(strategy)).Composition.HasCode();

	/// <summary>
	/// Check the specified <see cref="CompositionDiagramElement"/> contains code element.
	/// </summary>
	/// <param name="composition"><see cref="CompositionDiagramElement"/></param>
	/// <returns>Check result.</returns>
	public static bool HasCode(this CompositionDiagramElement composition)
		=> composition.FindAllElements().Any(e => e.IsExternalCode);

	/// <summary>
	/// Find all non <see cref="CompositionDiagramElement"/> elements.
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<DiagramElement> FindAllElements(this CompositionDiagramElement composition)
		=> composition.FindAllElements<DiagramElement>();

	/// <summary>
	/// Find all non <see cref="CompositionDiagramElement"/> elements.
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<T> FindAllElements<T>(this CompositionDiagramElement composition)
		where T : DiagramElement
	{
		if (composition is null)
			throw new ArgumentNullException(nameof(composition));

		foreach (var elem in composition.Elements)
		{
			if (elem is CompositionDiagramElement comp)
			{
				foreach (var item in FindAllElements<T>(comp))
					yield return item;
			}
			else if (elem is T typed)
				yield return typed;
		}
	}

	internal static object ConvertValue(this object value, Type destType, bool canNull)
	{
		if (destType is null)
			throw new ArgumentNullException(nameof(destType));

		if (value is null)
		{
			if (canNull)
				return null;

			throw new InvalidOperationException(LocalizedStrings.SocketNoValue);
		}

		var valueType = value.GetType();

		if (valueType.Is(destType))
			return value;

		decimal? toDecimal()
		{
			if (valueType.IsNumeric())
				return value.To<decimal>();

			return value switch
			{
				Unit unit => unit.To<decimal>(),
				string str => str.To<decimal>(),
				IIndicatorValue indValue => canNull && indValue.IsEmpty ? null : indValue.ToDecimal(),
				QuoteChange quote => quote.Price,
				ICandleMessage candle => candle.ClosePrice,
				IOrderBookMessage book => book.GetSpreadMiddle(default) ?? (canNull ? null : throw new InvalidOperationException(LocalizedStrings.MarketDepthIsEmpty)),
				DateTime dt => dt.ToUniversalTime().Ticks,
				DateTimeOffset dto => dto.UtcTicks,
				TimeSpan ts => ts.Ticks,
				CandleStates cs => (int)cs,
				OrderStates os => (int)os,
				bool b => b ? 1 : 0,
				_ => throw new ArgumentException($"Can't convert '{value}' to {destType}."),
			};
		}

		if (destType == typeof(decimal))
			return toDecimal();
		else if (destType == typeof(Unit))
		{
			if (value is string str)
				return str.ToUnit(false);

			return (Unit)toDecimal();
		}
		else if (destType == typeof(bool) || destType == typeof(bool?))
		{
			if (value is bool b)
				return b;
			else if (value is SingleIndicatorValue<bool> siv)
				return siv.Value;

			if (destType == typeof(bool?))
				return null;
		}

		return value.To(destType);
	}

	/// <summary>
	/// Determine the specified value is final.
	/// </summary>
	/// <param name="value"><see cref="DiagramSocketValue"/></param>
	/// <returns>Operation result.</returns>
	public static bool? IsFinal(this DiagramSocketValue value)
		=> value.Value switch
		{
			ICandleMessage c => c.State == CandleStates.Finished,
			IIndicatorValue v => v.IsFinal,
			IOrderBookMessage b => b.IsFinal(),

			_ => null,
		};

	/// <summary>
	/// Find the <see cref="DiagramSocketType"/> by the specified <see cref="Type"/>.
	/// </summary>
	/// <param name="type">Type.</param>
	/// <returns><see cref="DiagramSocketType"/></returns>
	public static DiagramSocketType ToDiagramType(this Type type)
		=> DiagramSocketType.AllTypes.FirstOrDefault(t => t.Type == type) ?? DiagramSocketType.Any;

	/// <summary>
	/// Get all sockets from the specified <see cref="CompositionDiagramElement"/>.
	/// </summary>
	/// <param name="composition"><see cref="CompositionDiagramElement"/></param>
	/// <returns>All sockets.</returns>
	public static IEnumerable<DiagramSocket> GetAllElementsSockets(this CompositionDiagramElement composition)
		=> composition.CheckOnNull(nameof(composition)).Elements.SelectMany(GetAllSockets);

	/// <summary>
	/// Get all sockets from the specified <see cref="DiagramElement"/>.
	/// </summary>
	/// <param name="element"><see cref="DiagramElement"/></param>
	/// <returns>All sockets.</returns>
	public static IEnumerable<DiagramSocket> GetAllSockets(this DiagramElement element)
		=> element.CheckOnNull(nameof(element)).InputSockets.Concat(element.OutputSockets);

	/// <summary>
	/// Filter the specified <paramref name="sockets"/> by the <see cref="DiagramSocket.IsSelected"/>.
	/// </summary>
	/// <param name="sockets">All sockets.</param>
	/// <returns>Selected sockets.</returns>
	public static IEnumerable<DiagramSocket> SelectedOnly(this IEnumerable<DiagramSocket> sockets)
		=> sockets.Where(s => s.IsSelected);

	/// <summary>
	/// Filter the specified <paramref name="sockets"/> by the <see cref="DiagramSocket.IsBreak"/>.
	/// </summary>
	/// <param name="sockets">All sockets.</param>
	/// <returns>Break sockets.</returns>
	public static IEnumerable<DiagramSocket> BreakOnly(this IEnumerable<DiagramSocket> sockets)
		=> sockets.Where(s => s.IsBreak);

	/// <summary>
	/// Find the element by the specified identifier.
	/// </summary>
	/// <param name="composition"><see cref="CompositionDiagramElement"/></param>
	/// <param name="elementId"><see cref="DiagramElement"/> identifier.</param>
	/// <param name="element">Found <see cref="DiagramElement"/>.</param>
	/// <returns>Operation result.</returns>
	public static bool TryGetElementById(this CompositionDiagramElement composition, Guid elementId, out DiagramElement element)
	{
		element = composition.Elements.FirstOrDefault(e1 => e1.Id == elementId);
		return element is not null;
	}

	/// <summary>
	/// Find the socket by the specified identifiers.
	/// </summary>
	/// <param name="composition"><see cref="CompositionDiagramElement"/></param>
	/// <param name="elementId"><see cref="DiagramElement"/> identifier.</param>
	/// <param name="socketId"><see cref="DiagramSocket"/> identifier.</param>
	/// <param name="socket">Found socket.</param>
	/// <returns>Operation result.</returns>
	public static bool TryGetSocketById(this CompositionDiagramElement composition, Guid elementId, string socketId, out DiagramSocket socket)
	{
		if (composition is null)
			throw new ArgumentNullException(nameof(composition));

		if (socketId.IsEmpty())
			throw new ArgumentNullException(nameof(socketId));

		socket = default;

		if (!composition.TryGetElementById(elementId, out var element))
			return false;

		socket = element.GetAllSockets().FirstOrDefault(s => s.Id.EqualsIgnoreCase(socketId));
		return socket is not null;
	}
}