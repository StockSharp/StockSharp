namespace StockSharp.Messages;

/// <summary>
/// Command types.
/// </summary>
[DataContract]
[Serializable]
public enum CommandTypes
{
	/// <summary>
	/// Start.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StartKey)]
	Start,

	/// <summary>
	/// Stop.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StopKey)]
	Stop,

	/// <summary>
	/// Enable.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EnableKey)]
	Enable,

	/// <summary>
	/// Disable.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DisableKey)]
	Disable,

	/// <summary>
	/// Update settings.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.InstallerUpdateKey)]
	Update,

	/// <summary>
	/// Add.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AddKey)]
	Add,

	/// <summary>
	/// Remove.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RemoveKey)]
	Remove,

	/// <summary>
	/// Request current state.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GetKey)]
	Get,

	/// <summary>
	/// Close position.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ClosePositionKey)]
	ClosePosition,

	/// <summary>
	/// Cancel orders.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CancelOrdersKey)]
	CancelOrders,

	/// <summary>
	/// Register new order.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RegisterOrderKey)]
	RegisterOrder,

	/// <summary>
	/// Cancel order.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CancelOrderKey)]
	CancelOrder,

	/// <summary>
	/// Restart.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RestartKey)]
	Restart,

	/// <summary>
	/// Share.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ShareKey)]
	Share,

	/// <summary>
	/// Unshare.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UnshareKey)]
	UnShare,

	/// <summary>
	/// List objects.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ListKey)]
	List,
}

/// <summary>
/// Command scopes.
/// </summary>
[DataContract]
[Serializable]
public enum CommandScopes
{
	/// <summary>
	/// Application.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ApplicationKey)]
	Application,

	/// <summary>
	/// Adapter.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AdapterKey)]
	Adapter,

	/// <summary>
	/// Strategy.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StrategyKey)]
	Strategy,

	/// <summary>
	/// Position.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PositionKey)]
	Position,

	/// <summary>
	/// Order.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderKey)]
	Order,

	/// <summary>
	/// File.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FileKey)]
	File,

	/// <summary>
	/// File group.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FileGroupKey)]
	FileGroup,

	/// <summary>
	/// Product.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ProductKey)]
	Product,

	/// <summary>
	/// License.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LicenseKey)]
	License,

	/// <summary>
	/// License feature.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LicenseFeatureKey)]
	LicenseFeature,

	/// <summary>
	/// Product category.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ProductCategoryKey)]
	ProductCategory,

	/// <summary>
	/// Product permission.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ProductPermissionKey)]
	ProductPermission,

	/// <summary>
	/// Product feedback.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ProductFeedbackKey)]
	ProductFeedback,
}

/// <summary>
/// The message contains information about command to change state.
/// </summary>
[Serializable]
[DataContract]
public class CommandMessage : BaseRequestMessage
{
	/// <summary>
	/// Initialize <see cref="CommandMessage"/>.
	/// </summary>
	public CommandMessage()
		: this(MessageTypes.Command)
	{
	}

	/// <summary>
	/// Initialize <see cref="CommandMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected CommandMessage(MessageTypes type)
		: base(type)
	{
	}

	/// <summary>
	/// Command.
	/// </summary>
	[DataMember]
	public CommandTypes Command { get; set; }

	/// <summary>
	/// Scope.
	/// </summary>
	[DataMember]
	public CommandScopes Scope { get; set; }

	/// <summary>
	/// Identifier.
	/// </summary>
	[DataMember]
	public string ObjectId { get; set; }

	[field: NonSerialized]
	private readonly Dictionary<string, string> _parameters = [];

	/// <summary>
	/// Parameters.
	/// </summary>
	[XmlIgnore]
	public IDictionary<string, string> Parameters => _parameters;

	/// <summary>
	/// Create a copy of <see cref="CommandMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new CommandMessage();

		CopyTo(clone);

		return clone;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	/// <returns>The object, to which copied information.</returns>
	protected void CopyTo(CommandMessage destination)
	{
		base.CopyTo(destination);

		destination.Command = Command;
		destination.Scope = Scope;
		destination.ObjectId = ObjectId;
		destination.Parameters.AddRange(Parameters);
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.Command;

	/// <inheritdoc />
	public override string ToString()
		=> base.ToString() + $",Cmd={Command},Scp={Scope},Id={ObjectId}";
}