namespace StockSharp.Messages;

/// <summary>
/// Validates that a <see cref="Unit"/> value is greater than zero.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class UnitGreaterThanZeroAttribute : ValidationAttribute
{
	/// <inheritdoc/>
	public override bool IsValid(object value)
		=> value is Unit u && u.Value > 0m;
}

/// <summary>
/// Validates that a <see cref="Unit"/> value is null or greater than zero.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class UnitNullOrMoreZeroAttribute : ValidationAttribute
{
	/// <inheritdoc/>
	public override bool IsValid(object value)
	{
		if (value is null) return true;
		return value is Unit u && u.Value > 0m;
	}
}

/// <summary>
/// Validates that a <see cref="Unit"/> value is null or not negative (zero or greater).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class UnitNullOrNotNegativeAttribute : ValidationAttribute
{
	/// <inheritdoc/>
	public override bool IsValid(object value)
	{
		if (value is null) return true;
		return value is Unit u && u.Value >= 0m;
	}
}

/// <summary>
/// Validates that a <see cref="Unit"/> value is not negative (zero or greater).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class UnitNotNegativeAttribute : ValidationAttribute
{
	/// <inheritdoc/>
	public override bool IsValid(object value)
		=> value is Unit u && u.Value >= 0m;
}

/// <summary>Range restriction for Unit values.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[CLSCompliant(false)]
public class UnitRangeAttribute : RangeAttribute, IValidator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UnitRangeAttribute"/>.
	/// </summary>
	/// <param name="min">Minimum value (inclusive).</param>
	/// <param name="max">Maximum value (inclusive).</param>
	public UnitRangeAttribute(Unit min, Unit max)
		: base((double)min.CheckOnNull(nameof(min)).Value, (double)max.CheckOnNull(nameof(max)).Value)
	{
		if (min.Type != max.Type)
			throw new ArgumentOutOfRangeException(nameof(max), max.Type, LocalizedStrings.InvalidValue);

		if (min.Value > max.Value)
			throw new ArgumentOutOfRangeException(nameof(min), min.Value, LocalizedStrings.InvalidValue);

		Min = min;
		Max = max;
	}

	/// <inheritdoc/>
	public bool DisableNullCheck { get; set; }

	/// <summary>
	/// Minimum value.
	/// </summary>
	public Unit Min { get; }

	/// <summary>
	/// Maximum value.
	/// </summary>
	public Unit Max { get; }

	/// <inheritdoc/>
	public override bool IsValid(object value)
	{
		if (value is null)
			return DisableNullCheck;

		if (value is not Unit u)
			return false;

		if (u.Type != Min.Type)
			return false;

		return u.Value >= Min.Value && u.Value <= Max.Value;
	}
}

/// <summary>Step restriction for Unit values (Value = Base + N*Step, N>=0).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[CLSCompliant(false)]
public class UnitStepAttribute : StepAttribute, IValidator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UnitStepAttribute"/>.
	/// </summary>
	/// <param name="step"><see cref="StepUnit"/></param>
	/// <param name="baseValue"><see cref="BaseValueUnit"/></param>
	public UnitStepAttribute(Unit step, Unit baseValue)
		: base(step.CheckOnNull(nameof(step)).Value, baseValue.CheckOnNull(nameof(baseValue)).Value)
	{
		if (step.Type != baseValue.Type)
			throw new ArgumentOutOfRangeException(nameof(baseValue), baseValue.Type, LocalizedStrings.InvalidValue);

		StepUnit = step;
		BaseValueUnit = baseValue;
	}

	/// <summary>
	/// Step value.
	/// </summary>
	public Unit StepUnit { get; }

	/// <summary>
	/// Base value.
	/// </summary>
	public Unit BaseValueUnit { get; }

	/// <inheritdoc/>
	public override bool IsValid(object value)
	{
		if (value is null)
			return DisableNullCheck;

		if (value is not Unit u || u.Type != StepUnit.Type)
			return false;

		var diff = u.Value - BaseValueUnit.Value;

		if (diff < 0m)
			return false;

		var q = diff / StepUnit.Value;
		var qRound = q.Round();
		return (q - qRound).Abs() < 1e-10m;
	}
}