using System;
using Ecng.Common;
using Ecng.ComponentModel;

namespace StockSharp.Algo;

/// <summary>
/// </summary>
public class FormulaEditorAttribute : Attribute
{
	/// <summary>
	/// Type to get variables from.
	/// </summary>
	public Type VariablesSource { get; }

	/// <summary>
	/// </summary>
	public FormulaEditorAttribute(Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		VariablesSource =
			type.Is<IItemsSource>()
				? type
				: type.IsEnum
					? typeof(ItemsSourceBase<>).Make(type)
					: throw new ArgumentException($"Type '{type}' must implement the '{typeof(IItemsSource)}' interface or be an enum.", nameof(type));
	}
}
