namespace StockSharp.Algo.Risk;

/// <summary>
/// Base risk-rule.
/// </summary>
public abstract class RiskRule : IRiskRule, INotifyPropertyChanged
{
	/// <summary>
	/// Initialize <see cref="RiskRule"/>.
	/// </summary>
	protected RiskRule()
	{
		UpdateTitle();
	}

	/// <summary>
	/// Get title.
	/// </summary>
	protected abstract string GetTitle();

	/// <summary>
	/// Update title.
	/// </summary>
	protected void UpdateTitle() => Title = GetTitle();

	private string _title;

	/// <summary>
	/// Header.
	/// </summary>
	[Browsable(false)]
	public string Title
	{
		get => _title;
		private set
		{
			_title = value;
			NotifyChanged();
		}
	}

	private RiskActions _action;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ActionKey,
		Description = LocalizedStrings.RiskRuleActionKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public RiskActions Action
	{
		get => _action;
		set
		{
			if (_action == value)
				return;

			_action = value;
			NotifyChanged();
		}
	}

	/// <inheritdoc />
	public virtual void Reset()
	{
	}

	/// <inheritdoc />
	public abstract bool ProcessMessage(Message message);

	/// <inheritdoc />
	public virtual Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		Action = storage.GetValue<RiskActions>(nameof(Action));

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public virtual Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.SetValue(nameof(Action), Action.To<string>());

		return Task.CompletedTask;
	}

	private PropertyChangedEventHandler _propertyChanged;

	event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
	{
		add => _propertyChanged += value;
		remove => _propertyChanged -= value;
	}

	private void NotifyChanged([CallerMemberName]string propertyName = null)
	{
		_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}