namespace StockSharp.Messages;

/// <summary>
/// Base order condition (for example, for stop order algo orders).
/// </summary>
[DataContract]
[Serializable]
public abstract class OrderCondition : Cloneable<OrderCondition>
{
	/// <summary>
	/// Initialize <see cref="OrderCondition"/>.
	/// </summary>
	protected OrderCondition()
	{
	}

	private readonly SynchronizedDictionary<string, object> _parameters = [];

	/// <summary>
	/// Condition parameters.
	/// </summary>
	[Browsable(false)]
	[DataMember]
	public IDictionary<string, object> Parameters
	{
		get => _parameters;
		set
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			_parameters.Clear();
			_parameters.AddRange(value);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="OrderCondition"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override OrderCondition Clone()
	{
		var clone = GetType().CreateInstance<OrderCondition>();
		clone.Parameters.Clear(); // removing pre-defined values
		clone.Parameters.AddRange(_parameters.SyncGet(d => d.Select(p => new KeyValuePair<string, object>(p.Key, p.Value is ICloneable cl ? cl.Clone() : (p.Value is IPersistable pers ? pers.Clone() : p.Value))).ToArray()));
		return clone;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return GetType().Name.Remove(nameof(OrderCondition)) + ": " + _parameters.SyncGet(d => d.Select(p => $"{p.Key}={p.Value}").JoinComma());
	}
}

/// <summary>
/// The interface describing REPO order condition.
/// </summary>
public interface IRepoOrderCondition
{
	/// <summary>
	/// REPO.
	/// </summary>
	bool IsRepo { get; set; }

	/// <summary>
	/// Information for REPO\REPO-M orders.
	/// </summary>
	RepoOrderInfo RepoInfo { get; set; }
}

/// <summary>
/// The interface describing NTM order condition.
/// </summary>
public interface INtmOrderCondition
{
	/// <summary>
	/// NTM.
	/// </summary>
	bool IsNtm { get; set; }

	/// <summary>
	/// Information for Negotiated Trades Mode orders.
	/// </summary>
	NtmOrderInfo NtmInfo { get; set; }
}

/// <summary>
/// The interface describing take-profit order condition.
/// </summary>
public interface ITakeProfitOrderCondition
{
	/// <summary>
	/// Close position price. <see langword="null"/> means close by market.
	/// </summary>
	decimal? ClosePositionPrice { get; set; }

	/// <summary>
	/// The absolute value of the price when the one is reached the protective strategy is activated.
	/// </summary>
	decimal? ActivationPrice { get; set; }
}

/// <summary>
/// The interface describing stop-loss order condition.
/// </summary>
public interface IStopLossOrderCondition
{
	/// <summary>
	/// Close position price. <see langword="null"/> means close by market.
	/// </summary>
	decimal? ClosePositionPrice { get; set; }

	/// <summary>
	/// The absolute value of the price when the one is reached the protective strategy is activated.
	/// </summary>
	decimal? ActivationPrice { get; set; }

	/// <summary>
	/// Trailing stop-loss.
	/// </summary>
	bool IsTrailing { get; set; }
}

/// <summary>
/// The interface describing withdraw funds condition.
/// </summary>
public interface IWithdrawOrderCondition
{
	/// <summary>
	/// Withdraw.
	/// </summary>
	bool IsWithdraw { get; set; }

	/// <summary>
	/// Withdraw info.
	/// </summary>
	WithdrawInfo WithdrawInfo { get; set; }
}

/// <summary>
/// The base implementation <see cref="IWithdrawOrderCondition"/>.
/// </summary>
[Serializable]
[DataContract]
public abstract class BaseWithdrawOrderCondition : OrderCondition, IWithdrawOrderCondition
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BaseWithdrawOrderCondition"/>.
	/// </summary>
	protected BaseWithdrawOrderCondition()
	{
		IsWithdraw = false;
		WithdrawInfo = new WithdrawInfo(); 
	}

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.WithdrawKey,
		Description = LocalizedStrings.WithdrawKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.WithdrawKey,
		Order = 10)]
	public bool IsWithdraw
	{
		get => (bool)Parameters[nameof(IsWithdraw)];
		set => Parameters[nameof(IsWithdraw)] = value;
	}

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.WithdrawInfoKey,
		Description = LocalizedStrings.WithdrawInfoKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.WithdrawKey,
		Order = 11)]
	public WithdrawInfo WithdrawInfo
	{
		get => (WithdrawInfo)Parameters[nameof(WithdrawInfo)];
		set => Parameters[nameof(WithdrawInfo)] = value ?? throw new ArgumentNullException(nameof(value));
	}
}

/// <summary>
/// Attribute, applied to <see cref="IMessageAdapter"/>, to provide information about type of <see cref="OrderCondition"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class OrderConditionAttribute : Attribute
{
	/// <summary>
	/// <see cref="OrderCondition"/> type.
	/// </summary>
	public Type ConditionType { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderConditionAttribute"/>.
	/// </summary>
	/// <param name="conditionType"><see cref="OrderCondition"/> type.</param>
	public OrderConditionAttribute(Type conditionType)
	{
		if (conditionType == null)
			throw new ArgumentNullException(nameof(conditionType));

		if (!conditionType.IsSubclassOf(typeof(OrderCondition)))
			throw new ArgumentException(conditionType.ToString());

		ConditionType = conditionType;
	}
}