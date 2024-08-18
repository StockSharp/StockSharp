namespace StockSharp.Algo;

using System;

using Ecng.Common;
using Ecng.ComponentModel;

/// <summary>
/// Formula editor attribute.
/// </summary>
public class FormulaEditorAttribute : Attribute
{
	/// <summary>
	/// Type to get variables from.
	/// </summary>
	public Type VariablesSource { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FormulaEditorAttribute"/>.
	/// </summary>
	/// <param name="type">Variables source.</param>
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