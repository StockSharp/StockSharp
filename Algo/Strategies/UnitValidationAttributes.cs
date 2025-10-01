namespace StockSharp.Algo.Strategies;

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

/// <summary>Step restriction for Unit values (Value = Base + N*Step, N>=0).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[CLSCompliant(false)]
public class UnitStepAttribute : ValidationAttribute, IValidator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UnitStepAttribute"/>.
	/// </summary>
	/// <param name="step"><see cref="Step"/></param>
	/// <param name="baseValue"><see cref="BaseValue"/></param>
	public UnitStepAttribute(Unit step, Unit baseValue)
	{
		if (step == null)
			throw new ArgumentNullException(nameof(step));
		
		if (step.Value <= 0m)
			throw new ArgumentOutOfRangeException(nameof(step), step, LocalizedStrings.InvalidValue);
		
		if (baseValue == null)
			throw new ArgumentNullException(nameof(baseValue));

		if (step.Type != baseValue.Type)
			throw new ArgumentOutOfRangeException(nameof(baseValue), baseValue.Type, LocalizedStrings.InvalidValue);

		Step = step;
		BaseValue = baseValue;
	}

	/// <inheritdoc/>
	public bool DisableNullCheck { get; set; }

	/// <summary>
	/// Step value.
	/// </summary>
	public Unit Step { get; }

	/// <summary>
	/// Base value.
	/// </summary>
	public Unit BaseValue { get; }

	/// <inheritdoc/>
	public override bool IsValid(object value)
	{
		if (value is null)
			return DisableNullCheck;

		if (value is not Unit u || u.Type != Step.Type)
			return false;

		var diff = u.Value - BaseValue.Value;

		if (diff < 0m)
			return false;

		var q = diff / Step.Value;
		var qRound = q.Round();
		return (q - qRound).Abs() < 1e-10m;
	}
}