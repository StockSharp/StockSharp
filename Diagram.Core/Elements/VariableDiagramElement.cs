namespace StockSharp.Diagram.Elements;

/// <summary>
/// Value storage element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.VariableKey,
	Description = LocalizedStrings.VariableElementDescriptionKey,
	GroupName = LocalizedStrings.SourcesKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html")]
public class VariableDiagramElement : TypedDiagramElement<VariableDiagramElement>
{
	private class VariableParameter(VariableDiagramElement element) : NotifiableObject, IDiagramElementParam
	{
		private const string _valueKey = nameof(Value);

		private readonly VariableDiagramElement _element = element ?? throw new ArgumentNullException(nameof(element));
		private string _securityId;
		private bool _newSecuritiesSubscribed;
		private bool _suspendPropertyChanged;
		private bool _hasValue;

		private string _name;

		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				NotifyChanged();
			}
		}

		private object _value;

		public object Value
		{
			get => _value;
			set
			{
				if (!_suspendPropertyChanged)
					NotifyChanging();

				_value = value;
				_hasValue = value != null;

				ValueChanged?.Invoke();

				if (!_suspendPropertyChanged)
					NotifyChanged();
			}
		}

		private Type _type;

		public Type Type
		{
			get => _type;
			set
			{
				_type = value;
				NotifyChanged();
			}
		}

		private readonly List<Attribute> _attributes = [];
		public IList<Attribute> Attributes => _attributes;

		public bool IsDefault => !_hasValue;

		public bool CanOptimize
		{
			get => true;
			set => throw new NotSupportedException();
		}

		public bool IgnoreOnSave { get; set; }

		bool IDiagramElementParam.NotifyOnChanged
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public event Action ValueChanged;

		public void SetValueWithIgnoreOnSave(object value)
		{
			IgnoreOnSave = true;
			Value = value;
		}

		public void Load(SettingsStorage storage)
		{
			try
			{
				if (Type == typeof(Strategy) || Type == typeof(IConnector))
				{
				}
				else if (Type == typeof(Security))
				{
					LoadSecurity(storage.GetValue<string>(_valueKey));
				}
				else if (Type == typeof(Portfolio))
				{
					LoadPortfolio(storage.GetValue<string>(_valueKey));
				}
				else if (!Type.IsPersistable())
				{
					if (storage.TryGetValue(_valueKey, out var value))
						Value = value.To(Type);
				}
				else
				{
					// TODO temporary fix for Unit, remove in the next versions
					var value = storage.TryGetValue(_valueKey);

					Value = value is SettingsStorage settingsStorage
						? settingsStorage.LoadEntire<IPersistable>()
						: value;
				}
			}
			catch (InvalidCastException excp)
			{
				ServicesRegistry.LogManager?.Application
					.LogDebug(LocalizedStrings.LoadingVariableErrorParams, excp);

				// после изменения типа данных, в настройках может храниться значение для другого типа.
				Value = null;
			}
		}

		public void Save(SettingsStorage storage)
		{
			switch (Value)
			{
				case null:
				case IConnector _:
				case Strategy _:
					break;

				case Security security:
					storage.SetValue(_valueKey, security.Id);
					break;
				case Portfolio pf:
					storage.SetValue(_valueKey, pf.Name);
					break;
				case IPersistable persistable:
					storage.SetValue(_valueKey, persistable.SaveEntire(false));
					break;
				default:
					storage.SetValue(_valueKey, Value);
					break;
			}
		}

		private void LoadSecurity(string id)
		{
			if (id.IsEmpty())
			{
				SetValueSuspended(null);
				return;
			}

			_securityId = id;

			var secProvider = ServicesRegistry.SecurityProvider;
			var exchangeInfoProvider = ServicesRegistry.ExchangeInfoProvider;
			var security = secProvider.LookupById(_securityId);

			if (security != null)
			{
				SetValueSuspended(security);
				return;
			}

			var secIdParts = _securityId.ToSecurityId();
			var tempSecurity = new Security
			{
				Id = _securityId,
				Code = secIdParts.SecurityCode,
				Board = exchangeInfoProvider.GetOrCreateBoard(secIdParts.BoardCode)
			};

			SetValueSuspended(tempSecurity);

			if (_newSecuritiesSubscribed)
				return;

			void NewSecurities(IEnumerable<Security> securities)
			{
				var sec = securities.FirstOrDefault(s => s.Id.EqualsIgnoreCase(_securityId));

				if (sec == null)
					return;

				SetValueSuspended(sec);
				secProvider.Added -= NewSecurities;
				_newSecuritiesSubscribed = false;
			}

			secProvider.Added += NewSecurities;

			_newSecuritiesSubscribed = true;
		}

		private void LoadPortfolio(string name)
		{
			SetValueSuspended(name.IsEmpty()
				? null
				: ((_element.Strategy?.PortfolioProvider) ?? ServicesRegistry.PortfolioProvider).LookupByPortfolioName(name));
		}

		private void SetValueSuspended(object value)
		{
			_suspendPropertyChanged = true;
			Value = value;
			_suspendPropertyChanged = false;
		}
	}

	private readonly HashSet<DiagramSocket> _triggerLinks = [];
	private readonly HashSet<DiagramSocket> _inputLinks = [];

	private bool _valueProcessed;
	private bool _isStarted;
	private object _currentValue;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "95B3AEFE-23FD-4CEE-B49E-09764F2AB2E2".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Pi";

	private readonly VariableParameter _value;

	/// <summary>
	/// The variable value.
	/// </summary>
	public object Value
	{
		get => _value.Value;
		set => _value.Value = value;
	}

	private readonly DiagramElementParam<bool> _inputAsTrigger;

	/// <summary>
	/// Raise output value when input updated.
	/// </summary>
	public bool InputAsTrigger
	{
		get => _inputAsTrigger.Value;
		set => _inputAsTrigger.Value = value;
	}

	static VariableDiagramElement() => SocketTypesSource.SetValues(DiagramSocketType.AllTypes);

	/// <summary>
	/// Initializes a new instance of the <see cref="VariableDiagramElement"/>.
	/// </summary>
	public VariableDiagramElement()
		: base(LocalizedStrings.Variable)
	{
		var triggerSocket = AddInput(StaticSocketIds.Trigger, LocalizedStrings.Trigger, DiagramSocketType.Any, OnProcessTrigger, int.MaxValue);

		triggerSocket.Connected += OnTriggerSocketConnected;
		triggerSocket.Disconnected += OnTriggerSocketDisconnected;

		_value = new(this)
		{
			Name = nameof(Value),
			Type = typeof(object),
			Attributes =
			{
				new DisplayAttribute
				{
					Name = LocalizedStrings.Value,
					Description = LocalizedStrings.ElemValue,
					GroupName = LocalizedStrings.Variable,
					Order = 10,
				},
				new BasicSettingAttribute(),
			}
		};
		_value.ValueChanged += OnValueChanged;
		AddParam(_value);

		_inputAsTrigger = AddParam<bool>(nameof(InputAsTrigger))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Variable, LocalizedStrings.InputAsTrigger, LocalizedStrings.InputAsTriggerDesc, 20);

		ShowParameters = true;
	}

	private void OnValueChanged()
	{
		if (_isStarted || _value.IgnoreOnSave)
			return;

		switch (Value)
		{
			case DateTime dt:
				SetElementName(dt.ToString("d"));
				break;
			case DateTimeOffset dto:
				SetElementName(dto.ToString("d"));
				break;
			default:
				SetElementName(Value?.ToString() ?? Type?.ToString());
				break;
		}
	}

	/// <inheritdoc />
	protected override void TypeChanged()
	{
		UpdateOutputSocketType();

		_value.Type = Type.Type.TryMakeNullable();

		Value = null;

		if (Type == DiagramSocketType.Security)
		{
			ShowParameters = true;
		}
		else if (Type == DiagramSocketType.Portfolio)
		{
			ShowParameters = true;
		}

		if (Type.IsEditable())
			_value.Attributes.RemoveWhere(a => a is BrowsableAttribute);
		else
			_value.Attributes.Add(new BrowsableAttribute(false));

		//_currentValue = Value;
	}

	/// <inheritdoc />
	protected override void OnInputSocketConnected(DiagramSocket socket, DiagramSocket source)
	{
		ShowParameters = false;

		if (socket.IsOutput)
			return;

		_inputLinks.Add(source);
	}

	/// <inheritdoc />
	protected override void OnInputSocketDisconnected(DiagramSocket socket, DiagramSocket source)
	{
		ShowParameters = true;

		if (socket.IsOutput)
			return;

		if (source != null)
			_inputLinks.Remove(source);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_valueProcessed = default;
		_isStarted = default;
		UpdateCurrentValue(null);
	}

	private void UpdateCurrentValue(object currentValue)
	{
		_currentValue = currentValue;
		FlushPriority = _currentValue is not null && _triggerLinks.IsEmpty() && _inputLinks.IsEmpty() ? FlushNormal : FlushDisabled;
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		base.OnStart(time);

		_isStarted = true;

		if (_valueProcessed)
			return;

		UpdateCurrentValue(Value);

		if (_currentValue == null)
		{
			if (Type == DiagramSocketType.Strategy)
			{
				UpdateCurrentValue(Strategy);
			}

			return;
		}

		if (Type == DiagramSocketType.Security)
		{
			if (_currentValue is null)
				throw new InvalidOperationException(LocalizedStrings.SecurityNotSpecified);

			//var security = (Security)_currentValue ?? throw new InvalidOperationException(LocalizedStrings.SecurityNotSpecified);
			//Strategy.LookupSecurities(security.ToLookupMessage());
		}
		else if (Type == DiagramSocketType.Portfolio)
		{
			if (_currentValue is null)
				throw new InvalidOperationException(LocalizedStrings.PortfolioNotSpecified);
		}

		if (FlushPriority == 0)
			RaiseProcessOutput(OutputSocket, time, _currentValue);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		_isStarted = false;
		base.OnStop();
	}

	/// <inheritdoc />
	protected override void OnProcess(DiagramSocketValue value)
	{
		_valueProcessed = true;
		UpdateCurrentValue(value.Value);

		if (InputAsTrigger)
			RaiseProcessOutput(OutputSocket, value.Time, _currentValue, value);
	}

	/// <inheritdoc />
	public override void Flush(DateTimeOffset time)
	{
		RaiseProcessOutput(OutputSocket, time, _currentValue);
	}

	private void OnProcessTrigger(DiagramSocketValue value)
	{
		if (!_valueProcessed && _currentValue == null)
			return;

		if (value.GetValue<bool?>() == false)
			return;

		RaiseProcessOutput(OutputSocket, value.Time, _currentValue, value);
	}

	private void OnTriggerSocketConnected(DiagramSocket socket, DiagramSocket source)
	{
		if (socket.IsOutput)
			return;

		_triggerLinks.Add(source);
	}

	private void OnTriggerSocketDisconnected(DiagramSocket socket, DiagramSocket source)
	{
		if (socket.IsOutput)
			return;

		if (source != null)
			_triggerLinks.Remove(source);
	}
}