namespace StockSharp.Diagram;

using System.Reflection;

/// <summary>
/// <see cref="DiagramSocket"/> breakpoint.
/// </summary>
/// <remarks>
/// Initialize <see cref="DiagramSocketBreakpoint"/>.
/// </remarks>
/// <param name="socket">Diagram socket.</param>
public class DiagramSocketBreakpoint(DiagramSocket socket) : IPersistable
{
	/// <summary>
	/// Diagram socket.
	/// </summary>
	[Browsable(false)]
	public DiagramSocket Socket { get; set; } = socket ?? throw new ArgumentNullException(nameof(socket));

	/// <summary>
	/// Whether need to break on socket.
	/// </summary>
	/// <returns>Check result.</returns>
	public bool NeedBreak()
	{
		var value = Socket.Value;

		if (value == null)
			return false;

		return OnNeedBreak(value);
	}

	/// <summary>
	/// Whether need to break on socket.
	/// </summary>
	/// <param name="value">Current value.</param>
	/// <returns>Check result.</returns>
	protected virtual bool OnNeedBreak(object value)
	{
		return true;
	}

	#region IPersistable

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		if (storage == null)
			throw new ArgumentNullException(nameof(storage));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		if (storage == null)
			throw new ArgumentNullException(nameof(storage));
	}

	#endregion
}

[Obfuscation(Feature = "properties renaming")]
class RangeDiagramSocketBreakpoint<TValue>(DiagramSocket socket) : DiagramSocketBreakpoint(socket)
	where TValue : struct, IComparable
{
	[Display(
		ResourceType = typeof(LocalizedStrings),
		GroupName = LocalizedStrings.CommonKey,
		Name = LocalizedStrings.MinimumKey,
		Description = LocalizedStrings.MinimumKey,
		Order = 10)]
	public TValue? MinValue { get; set; }

	[Display(
		ResourceType = typeof(LocalizedStrings),
		GroupName = LocalizedStrings.CommonKey,
		Name = LocalizedStrings.MaximumKey,
		Description = LocalizedStrings.MaximumKey,
		Order = 10)]
	public TValue? MaxValue { get; set; }

	protected override bool OnNeedBreak(object value)
	{
		var valueType = typeof(TValue);

		if (valueType == typeof(decimal))
		{
			if (value is not Unit && value is not IIndicatorValue && !value.GetType().IsNumeric())
				return false;
		}
		else if (valueType.IsDateTime())
		{
			if (value?.GetType() != valueType)
				return false;
		}
		else if (valueType == typeof(TimeSpan))
		{
			if (value is not TimeSpan)
				return false;
		}

		var comparable = (IComparable)value;

		if (MinValue != null)
		{
			IComparable min = MinValue.Value;

			if (!valueType.IsDateOrTime())
				CompositionHelper.CheckForValueTypes(ref comparable, ref min);

			if (comparable.Compare(min) < 0)
				return false;
		}

		if (MaxValue != null)
		{
			IComparable max = MaxValue.Value;

			if (!valueType.IsDateOrTime())
				CompositionHelper.CheckForValueTypes(ref comparable, ref max);

			if (comparable.Compare(max) > 0)
				return false;
		}

		return true;
	}

	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		MinValue = storage.GetValue(nameof(MinValue), MinValue);
		MaxValue = storage.GetValue(nameof(MaxValue), MaxValue);
	}

	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(MinValue), MinValue);
		storage.SetValue(nameof(MaxValue), MaxValue);
	}
}

[Obfuscation(Feature = "properties renaming")]
class BooleanDiagramSocketBreakpoint(DiagramSocket socket) : DiagramSocketBreakpoint(socket)
{
	[Display(
		ResourceType = typeof(LocalizedStrings),
		GroupName = LocalizedStrings.CommonKey,
		Name = LocalizedStrings.ValueKey,
		Description = LocalizedStrings.ValueKey,
		Order = 10)]
	public bool? Value { get; set; }

	protected override bool OnNeedBreak(object value)
	{
		if (Value == null)
			return true;

		return value as bool? == Value;
	}

	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Value = storage.GetValue(nameof(Value), Value);
	}

	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Value), Value);
	}
}

[Obfuscation(Feature = "properties renaming")]
class EnumDiagramSocketBreakpoint<TEnum>(DiagramSocket socket) : DiagramSocketBreakpoint(socket)
	where TEnum : struct
{
	[Display(
		ResourceType = typeof(LocalizedStrings),
		GroupName = LocalizedStrings.CommonKey,
		Name = LocalizedStrings.ValueKey,
		Description = LocalizedStrings.ValueKey,
		Order = 10)]
	public TEnum? Value { get; set; }

	protected override bool OnNeedBreak(object value)
	{
		if (Value == null)
			return true;

		return (value as TEnum?)?.Equals(Value) == true;
	}

	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Value = storage.GetValue(nameof(Value), Value);
	}

	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Value), Value);
	}
}