namespace StockSharp.Diagram.Elements;

using Ecng.Reflection;

/// <summary>
/// Composite value of a complex object receiving element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ConverterKey,
	Description = LocalizedStrings.ConverterDescriptionKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/converters/converter.html")]
public class ConverterDiagramElement : TypedDiagramElement<ConverterDiagramElement>
{
	/// <inheritdoc />
	public override Guid TypeId { get; } = "35C046C4-1B0F-4074-AC08-0C70850A8DCF".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Sort";

	private readonly DiagramElementParam<string> _property;

	/// <summary>
	/// Property.
	/// </summary>
	public string Property
	{
		get => _property.Value;
		set => _property.Value = value;
	}

	static ConverterDiagramElement()
	{
		SocketTypesSource.SetValues(DiagramSocketType.AllTypes.Where(t =>
			(!t.Type.IsPrimitive() || t.Type == typeof(DateTimeOffset)) &&
			!t.Type.IsEnum() &&
			t.Type != typeof(Unit) &&
			t.Type != typeof(object) &&
			(t.Type == typeof(IOrderBookMessage) || !t.Type.IsCollection()) &&
			t.Type != typeof(IComparable)));
	}

	private readonly DiagramSocket _inputSocket;
	private readonly Dictionary<string, DiagramSocket> _varSockets = [];

	private IndicatorDiagramElement _indicatorSource;
	private ConverterDiagramElement _converterSource;

	private Type _indicatorType;

	/// <summary>
	/// Initializes a new instance of the <see cref="ConverterDiagramElement"/>.
	/// </summary>
	public ConverterDiagramElement()
		: base(LocalizedStrings.Converter, true)
	{
		_inputSocket = InputSockets.First();

		_inputSocket.CanConnectEx += CanConnectEx;

		_property = AddParam<string>(nameof(Property))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Converter, LocalizedStrings.Property, LocalizedStrings.ObjectPropertyValue, 20)
			.SetEditor(new TemplateEditorAttribute { TemplateKey = "EntityPropertiesEditorTemplate" })
			.SetOnValueChangedHandler(v =>
			{
				var socket = OutputSockets.First();

				void clearVarSockets(DiagramSocketType outputType)
				{
					socket.Type = outputType;

					foreach (var socket in _varSockets.Values)
						RemoveSocket(socket);

					_varSockets.Clear();
				}

				SetElementName(v);

				if (Type != null && v != null)
				{
					if (Type == DiagramSocketType.IndicatorValue)
					{
						clearVarSockets(DiagramSocketType.IndicatorValue);
					}
					else
					{
						var propType = Type.Type.GetPropType(v, (t, n) => t.TryGetVirtualPropertyType(n, out var pt) ? pt : null);

						if (propType != null)
						{
							socket.Type = DiagramSocketType.GetSocketType(propType);

							var vars = Type.Type.GetVars(v, (t, n) => t.TryGetVirtualPropertyType(n, out var pt) ? pt : null);

							var toRemove = _varSockets.Keys.ToHashSet();

							foreach (var var in vars)
							{
								if (_varSockets.ContainsKey(var))
									continue;

								var socketId = GenerateSocketId(var);

								var varSocket = AddInput(socketId, var, DiagramSocketType.Unit);
								_varSockets.Add(var, varSocket);

								toRemove.Remove(var);
							}

							foreach (var r in toRemove)
							{
								RemoveSocket(_varSockets.GetAndRemove(r));
							}
						}
						else
						{
							clearVarSockets(DiagramSocketType.Any);
						}
					}
				}
				else
				{
					clearVarSockets(DiagramSocketType.Any);
				}

				RaiseSocketChanged(socket);
			});
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_inputSocket.CanConnectEx -= CanConnectEx;
		base.DisposeManaged();
	}

	private bool CanConnectEx(DiagramSocket from)
	{
		if (_indicatorType is not null && from.Parent is IndicatorDiagramElement ide && ide.Type?.Indicator != _indicatorType)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Get source indicator if any.
	/// </summary>
	public Type SourceIndicator => _indicatorType;

	/// <inheritdoc />
	protected override void OnInputSocketConnected(DiagramSocket socket, DiagramSocket source)
	{
		base.OnInputSocketConnected(socket, source);
		SubscribeIndicatorElement(source);
	}

	/// <inheritdoc />
	protected override void OnInputSocketDisconnected(DiagramSocket socket, DiagramSocket source)
	{
		base.OnInputSocketDisconnected(socket, source);
		UnsubscribeDiagramElement();
	}

	private void SubscribeIndicatorElement(DiagramSocket sourceSocket)
	{
		UnsubscribeDiagramElement();

		if (sourceSocket.Parent is IndicatorDiagramElement indicatorElem)
		{
			_indicatorSource = indicatorElem;
			_indicatorSource.ParameterValueChanged += IndicatorSource_OnParameterValueChanged;

			UpdateIndicatorType();
		}
		else if (sourceSocket.Parent is ConverterDiagramElement converterElem)
		{
			_converterSource = converterElem;
			_converterSource.ParameterValueChanged += ConverterSource_OnParameterValueChanged;

			UpdateConverterType();
		}
	}

	private void UnsubscribeDiagramElement()
	{
		if (_indicatorSource is not null)
		{
			_indicatorSource.ParameterValueChanged -= IndicatorSource_OnParameterValueChanged;
			_indicatorSource = null;
		}

		if (_converterSource is not null)
		{
			_converterSource.ParameterValueChanged -= ConverterSource_OnParameterValueChanged;
			_converterSource = null;
		}
	}

	private void IndicatorSource_OnParameterValueChanged(string name)
	{
		if (name == nameof(_indicatorSource.Type))
			UpdateIndicatorType();
	}

	private void ConverterSource_OnParameterValueChanged(string name)
	{
		if (name == nameof(_converterSource.Property) || name == nameof(_converterSource.Type))
			UpdateConverterType();
	}

	private void UpdateConverterType()
	{
		if (_converterSource.SourceIndicator is not null && !_converterSource.Property.IsEmpty())
			_indicatorType = _converterSource.SourceIndicator.GetPropType(_converterSource.Property, (v, n) => v.TryGetVirtualPropertyType(n, out var pv) ? pv : null);
		else
			_indicatorType = null;
	}

	private void UpdateIndicatorType()
	{
		var indicatorType = _indicatorSource.Type?.Indicator;
		if (_indicatorType == indicatorType)
			return;

		var changed = _indicatorType != null;
		_indicatorType = indicatorType;

		if (changed)
			TypeChanged();
		else
			OutputSockets.First().Type = DiagramSocketType.IndicatorValue;
	}

	/// <inheritdoc />
	protected override void TypeChanged() => Property = null;

	private string EnsureGetProperty()
	{
		var prop = Property;

		if (prop.IsEmpty())
			throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Property));

		return prop;
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		EnsureGetProperty();

		base.OnPrepare();
	}

	/// <inheritdoc />
	protected override void OnProcess(DiagramSocketValue value)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		var nextValue = values[_inputSocket].Value;

		var valuesByName = values.Where(p => p.Key != _inputSocket).ToDictionary(p => p.Key.Name, p => (object)(int)p.Value.GetValue<decimal>());

		object getPropValue(object entity, string propName)
			=> entity.GetPropValue(propName, (v, n) => v.TryGetVirtualValue(n, out var pv) ? pv : null, valuesByName);

		if (nextValue is IComplexIndicatorValue complex)
		{
			object getIndicatorValue()
			{
				object value = complex;

				var name = EnsureGetProperty();
				var parts = name.Split('.');

				for (var i = 0; i < parts.Length; ++i)
				{
					if (value is not IComplexIndicatorValue complexValue)
					{
						if (i == parts.Length - 1)
							break;

						throw new InvalidOperationException($"unexpected indicator value type. name='{name}', i={i}, type='{value?.GetType()}'");
					}

					var obj = getPropValue(complexValue, parts[i]);

					if (obj is null)
						return null;

					value = obj;
				}

				return value;
			}

			nextValue = getIndicatorValue();
		}
		else
		{
			nextValue = getPropValue(nextValue, EnsureGetProperty());
		}

		if (nextValue is null)
			return;

		RaiseProcessOutput(OutputSocket, time, nextValue, source);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		if (Scope<CopyPasteContext>.IsDefined)
			storage.SetValue(nameof(_indicatorType), _indicatorType);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		if (Scope<CopyPasteContext>.IsDefined)
			_indicatorType = storage.GetValue<Type>(nameof(_indicatorType));

		base.Load(storage);
	}
}