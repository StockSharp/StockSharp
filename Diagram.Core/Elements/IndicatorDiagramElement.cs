namespace StockSharp.Diagram.Elements;

using System.Reflection;

using Ecng.Reflection;

using StockSharp.Algo.Candles.Patterns;

/// <summary>
/// Indicator element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.IndicatorKey,
	Description = LocalizedStrings.IndicatorElementDescriptionKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/indicator.html")]
public class IndicatorDiagramElement : DiagramElement
{
	private IDiagramElementParam[] _indicatorParams = [];

	private DiagramSocket _additionalInput;
	private readonly DiagramSocket _outputSocket;
	private DiagramSocketValue _value;
	private DiagramSocketValue _additionalValue;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "F56C74AF-7C39-464A-8B90-4EFF4C8760B1".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "LineChart";

	private readonly DiagramElementParam<IndicatorType> _type;

	/// <summary>
	/// Indicator type.
	/// </summary>
	public IndicatorType Type
	{
		get => _type.Value;
		set => _type.Value = value;
	}

	/// <summary>
	/// The indicator parameters.
	/// </summary>
	public IIndicator Indicator { get; set; }

	private readonly DiagramElementParam<bool> _isFinal;

	/// <summary>
	/// Send only final values.
	/// </summary>
	public bool IsFinal
	{
		get => _isFinal.Value;
		set => _isFinal.Value = value;
	}

	private readonly DiagramElementParam<bool> _isFormed;

	/// <summary>
	/// Send values only when the indicator is formed.
	/// </summary>
	public bool IsFormed
	{
		get => _isFormed.Value;
		set => _isFormed.Value = value;
	}

	private readonly DiagramElementParam<bool> _isEmpty;

	/// <summary>
	/// Send empty indicator values.
	/// </summary>
	public bool IsEmpty
	{
		get => _isEmpty.Value;
		set => _isEmpty.Value = value;
	}

	private const string _input2Id = "input2";

	private static readonly MethodInfo _createParamMethod;

	static IndicatorDiagramElement()
	{
		_createParamMethod = typeof(IndicatorDiagramElement).GetMember<MethodInfo>(nameof(CreateParam));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorDiagramElement"/>.
	/// </summary>
	public IndicatorDiagramElement()
	{
		AddInput(StaticSocketIds.Input, LocalizedStrings.Input, DiagramSocketType.Any, OnProcess);
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Output, DiagramSocketType.IndicatorValue);

		void indicatorChanged()
		{
			var type = _type.Value;

			if (type is null)
				return;

			clearSockets();
			setIndicator(type);
		}

		void clearSockets()
		{
			_indicatorParams.ForEach(RemoveParam);

			if (_additionalInput != null)
			{
				RemoveSocket(_additionalInput);
				_additionalInput = null;
			}
		}

		void setIndicator(IndicatorType value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			var indicator = Scope<IIndicator>.Current?.Value ?? value.TryCreateIndicator();
			Indicator = indicator;

			if (value.InputValue == typeof(PairIndicatorValue<decimal>))
				_additionalInput = AddInput(GenerateSocketId(_input2Id), LocalizedStrings.Input, DiagramSocketType.Any, OnProcessAdditionalValue);

			_indicatorParams = indicator is null ? [] : [.. GetIndicatorParams(value.Indicator, indicator, order: 20)];
			_indicatorParams.ForEach(AddParam);

			if (indicator is not null)
				SetElementName(indicator.ToString());
		}

		_type = AddParam<IndicatorType>(nameof(Type))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Indicator, LocalizedStrings.Type, LocalizedStrings.IndicatorType, 10)
			.SetEditor(new EditorAttribute(typeof(IIndicatorProvider), typeof(IIndicatorProvider)))
			.SetOnValueChangingHandler((oldValue, newValue) =>
			{
				if (oldValue is not null)
					oldValue.IndicatorChanged -= indicatorChanged;

				if (newValue is not null)
					newValue.IndicatorChanged += indicatorChanged;
			})
			.SetOnValueChangedHandler(value =>
			{
				clearSockets();

				if (value is not null)
				{
					setIndicator(value);
				}
				else
				{
					SetElementName(null);
					Indicator = null;
				}

				RaisePropertiesChanged();
			})
			.SetSaveLoadHandlers(t =>
			{
				if (t is null)
					return null;

				return new SettingsStorage()
					.Set(nameof(t.Id), t.Id);
			}, s =>
			{
				if (s.ContainsKey("settings"))
					s = s.GetValue<SettingsStorage>("settings");

				var id = s.GetValue<string>(nameof(IndicatorType.Id)) ?? s.GetValue<string>(nameof(IndicatorType.Indicator));

				if (id.IsEmpty())
					return null;

				return IChartExtensions.IndicatorProvider.TryGetById(id);
			});

		_type.ValueValidating = (oldValue, newValue) => Scope<IIndicator>.IsDefined || oldValue != newValue;

		_isFinal = AddParam(nameof(IsFinal), false)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Indicator, LocalizedStrings.Final, LocalizedStrings.SendOnlyFinal, 80);

		_isFormed = AddParam(nameof(IsFormed), false)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Indicator, LocalizedStrings.Formed, LocalizedStrings.SendOnlyFormedIndicators, 90);

		_isEmpty = AddParam(nameof(IsEmpty), false)
			.SetDisplay(LocalizedStrings.Indicator, LocalizedStrings.Empty, LocalizedStrings.SendEmptyIndicatorValues, 100);

		ShowParameters = true;
	}

	private IEnumerable<IDiagramElementParam> GetIndicatorParams(Type indicatorType, IIndicator indicator, string name = "", string prefix = "", int order = 0)
	{
		var parameters = new List<IDiagramElementParam>();

		var props = (ServicesRegistry.TryCustomTypeDescriptorProvider?.TryGet(indicatorType, indicator, out var descriptor) == true
			? descriptor.GetProperties()
			: TypeDescriptor.GetProperties(indicatorType)
		).Typed();

		foreach (var propertyInfo in props.Where(p => p.IsBrowsable && !p.IsReadOnly))
		{
			IDiagramElementParam createParam(Type propType)
			{
				var getter = propertyInfo.GetValue;
				var setter = propertyInfo.SetValue;
				return (IDiagramElementParam)_createParamMethod.Make(propType).Invoke(this, [indicator, getter, setter, name, propertyInfo.Name, propertyInfo.DisplayName, prefix, order + parameters.Count]);
			}

			var propType = propertyInfo.PropertyType;
			propType = propType.GetUnderlyingType() ?? propType;

			if (propType.IsPrimitive ||
				propType == typeof(decimal) ||
				propType == typeof(Unit) ||
				propType == typeof(TimeSpan) ||
				propType == typeof(DateTime) ||
				propType == typeof(DateTimeOffset) ||
				propType.IsEnum)
			{
				parameters.Add(createParam(propType));
			}
			else if (propType.Is<IIndicator>())
			{
				var innerIndicator = (IIndicator)propertyInfo.GetValue(indicator);
				var displayName = propertyInfo.GetDisplayName();

				parameters.AddRange(GetIndicatorParams(innerIndicator.GetType(), innerIndicator, name + propertyInfo.Name + ".", prefix + displayName + " -> ", order + parameters.Count));
			}
			else if (propType.Is<ICandlePattern>())
			{
				const string PatternName = nameof(PatternName);
				const string Pattern = nameof(Pattern);

				var param = ((DiagramElementParam<ICandlePattern>)createParam(typeof(ICandlePattern)))
					.SetSaveLoadHandlers(pattern =>
					{
						var ss = new SettingsStorage();

						if (pattern is not null)
						{
							ss
								.Set(PatternName, pattern.Name)
								.Set(Pattern, pattern.SaveEntire(false))
							;
						}

						return ss;
					},
					storage =>
					{
						var patternName = storage.GetValue<string>(PatternName);
						if (patternName.IsEmptyOrWhiteSpace())
							return null;

						var provider = ServicesRegistry.TryCandlePatternProvider ?? throw new InvalidOperationException("candle pattern provider not found");

						if (!provider.TryFind(patternName, out var pattern))
						{
							if (!storage.ContainsKey(Pattern))
							{
								LogError($"pattern '{patternName}' not found");
							}
							else
							{
								pattern = storage.GetValue<SettingsStorage>(Pattern).LoadEntire<ICandlePattern>();
								provider.Save(pattern);
							}
						}

						return pattern;
					});

				parameters.Add(param);
			}
		}

		return parameters;
	}

	[Obfuscation(Feature = "renaming")]
	private DiagramElementParam<T> CreateParam<T>(IIndicator indicator,
		Func<object, object> getValue, Action<object, object> setValue,
		string name, string propName, string displayName, string prefix, int order)
	{
		if (indicator is null)	throw new ArgumentNullException(nameof(indicator));
		if (getValue is null)	throw new ArgumentNullException(nameof(getValue));
		if (setValue is null)	throw new ArgumentNullException(nameof(setValue));

		var value = getValue(indicator).To<T>();

		return new DiagramElementParam<T> { Name = name + propName, Value = value }
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Indicator, prefix + displayName, prefix + displayName + ".", order)
			.SetCanOptimize()
			.SetOnValueChangedHandler(v =>
			{
				setValue(indicator, v);
				SetElementName(indicator.ToString());
			});
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Indicator?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		Strategy.Indicators.Add(Indicator ?? throw new InvalidOperationException(LocalizedStrings.NotInitializedParams.Put(LocalizedStrings.Indicator)));

		base.OnStart(time);
	}

	private void OnProcess(DiagramSocketValue value)
	{
		if (Indicator is null)
			return;

		if (_additionalInput != null)
		{
			_value = value;
			OnProcessValue(value.Time, value);
		}
		else
			OnProcessValue(value.Time, value.Value, value);
	}

	private void OnProcessAdditionalValue(DiagramSocketValue value)
	{
		if (Indicator is null)
			return;

		_additionalValue = value;
		OnProcessValue(value.Time, value);
	}

	private void OnProcessValue(DateTimeOffset time, DiagramSocketValue source)
	{
		if (_value == null || _additionalValue == null)
			return;

		var item1 = _value.GetValue<decimal>();
		var item2 = _additionalValue.GetValue<decimal>();

		_value = null;
		_additionalValue = null;

		OnProcessValue(time, Tuple.Create(item1, item2), source);
	}

	private void OnProcessValue(DateTimeOffset time, object inputValue, DiagramSocketValue source)
	{
		var result = Indicator.Process(inputValue, time, true);

		if (IsFinal && !result.IsFinal)
			return;

		if (IsFormed && !result.IsFormed)
			return;

		if (!IsEmpty && result.IsEmpty)
			return;

		RaiseProcessOutput(_outputSocket, time, result, source);
	}
}