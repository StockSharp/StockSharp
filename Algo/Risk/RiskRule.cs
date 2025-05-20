namespace StockSharp.Algo.Risk;

using System.Runtime.CompilerServices;

/// <summary>
/// Base risk-rule.
/// </summary>
public abstract class RiskRule : BaseLogReceiver, IRiskRule, INotifyPropertyChanged
{
	/// <summary>
	/// Initialize <see cref="RiskRule"/>.
	/// </summary>
	protected RiskRule()
	{
		UpdateTitle();
	}

	/// <inheritdoc/>
	[Browsable(false)]
	public override Guid Id { get => base.Id; set => base.Id = value; }

	/// <inheritdoc/>
	[Browsable(false)]
	public override string Name { get => base.Name; set => base.Name = value; }

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
	public override void Load(SettingsStorage storage)
	{
		Action = storage.GetValue<RiskActions>(nameof(Action));

		base.Load(storage);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Action), Action.To<string>());

		base.Save(storage);
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