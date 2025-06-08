namespace StockSharp.Diagram;

/// <summary>
/// The diagram element parameter.
/// </summary>
public interface IDiagramElementParam : IPersistable, INotifyPropertyChanging, INotifyPropertyChanged, IAttributesEntity
{
	/// <summary>
	/// Parameter name.
	/// </summary>
	string Name { get; set; }

	/// <summary>
	/// Parameter type.
	/// </summary>
	Type Type { get; }

	/// <summary>
	/// The parameter value.
	/// </summary>
	object Value { get; set; }

	/// <summary>
	/// The default value is specified.
	/// </summary>
	bool IsDefault { get; }

	/// <summary>
	/// The parameter can be used in optimization.
	/// </summary>
	bool CanOptimize { get; set; }

	/// <summary>
	/// To ignore when saving.
	/// </summary>
	bool IgnoreOnSave { get; set; }

	/// <summary>
	/// Set value and ignore it on save settings.
	/// </summary>
	/// <param name="value">Value.</param>
	void SetValueWithIgnoreOnSave(object value);

	/// <summary>
	/// Raise changed event when property is changed.
	/// </summary>
	bool NotifyOnChanged { get; set; }
}
