namespace StockSharp.Algo;

/// <summary>
/// Attribute, applied to derived from <see cref="BasketSecurity"/> class, to provide basket type code.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BasketCodeAttribute : Attribute
{
	/// <summary>
	/// Basket type code.
	/// </summary>
	public string Code { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketCodeAttribute"/>.
	/// </summary>
	/// <param name="code">Basket type code.</param>
	public BasketCodeAttribute(string code)
	{
		if (code.IsEmpty())
			throw new ArgumentNullException(nameof(code));

		Code = code;
	}
}

/// <summary>
/// Instruments basket.
/// </summary>
[System.Runtime.Serialization.DataContract]
[Serializable]
public abstract class BasketSecurity : Security
{
	/// <summary>
	/// Initialize <see cref="BasketSecurity"/>.
	/// </summary>
	protected BasketSecurity()
	{
	}

	/// <summary>
	/// Instruments, from which this basket is created.
	/// </summary>
	[Browsable(false)]
	public abstract IEnumerable<SecurityId> InnerSecurityIds { get; }

	/// <inheritdoc />
	public override string BasketCode => GetType().GetAttribute<BasketCodeAttribute>().Code;

	/// <inheritdoc />
	public override string BasketExpression
	{
		get => ToSerializedString();
		set => FromSerializedString(value);
	}

	/// <summary>
	/// Save security state to string.
	/// </summary>
	/// <returns>String.</returns>
	protected abstract string ToSerializedString();

	/// <summary>
	/// Load security state from <paramref name="text"/>.
	/// </summary>
	/// <param name="text">Value, received from <see cref="ToSerializedString"/>.</param>
	protected abstract void FromSerializedString(string text);
}