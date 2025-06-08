namespace StockSharp.Diagram;

using System.Runtime.CompilerServices;

using Ecng.Configuration;

/// <summary>
/// Connection.
/// </summary>
public class DiagramSocket : Disposable, INotifyPropertyChanged
{
	/// <summary>
	/// Gui wrapper for property binding.
	/// </summary>
	public INotifyPropertyChanged GuiWrapper { get; }

	private string _id;

	/// <summary>
	/// The connection identifier.
	/// </summary>
	public string Id
	{
		get => _id;
		protected set
		{
			if (_id.EqualsIgnoreCase(value))
				return;

			_id = value;
			OnPropertyChanged();
		}
	}

	private string _name;

	/// <summary>
	/// The connection name.
	/// </summary>
	public string Name
	{
		get => _name;
		set
		{
			if (_name == value)
				return;

			_name = value;
			OnPropertyChanged();
		}
	}

	private DiagramSocketType _type = DiagramSocketType.Bool;

	/// <summary>
	/// Connection type.
	/// </summary>
	public DiagramSocketType Type
	{
		get => _type;
		set
		{
			if (_type == value)
				return;

			_type = value ?? throw new ArgumentNullException(nameof(value));
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// The connection direction.
	/// </summary>
	public DiagramSocketDirection Directon { get; }

	/// <summary>
	/// Dynamic sockets are removed during Load().
	/// </summary>
	public bool IsDynamic { get; set; }

	private int _linkableMaximum;

	/// <summary>
	/// The maximum number of connections.
	/// </summary>
	public int LinkableMaximum
	{
		get => _linkableMaximum;
		set
		{
			if (_linkableMaximum == value)
				return;

			_linkableMaximum = value;
			OnPropertyChanged();
		}
	}

	private object _value;

	/// <summary>
	/// The current value.
	/// </summary>
	public object Value
	{
		get => _value;
		set
		{
			_value = value;
			OnPropertyChanged();
		}
	}

	private DiagramElement _parent;

	/// <summary>
	/// The socket parent element.
	/// </summary>
	public DiagramElement Parent
	{
		get => _parent;
		set
		{
			if (_parent == value)
				return;

			_parent = value;
			OnPropertyChanged();
		}
	}

	private bool _isSelected;

	/// <summary>
	/// Is socket selected.
	/// </summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (_isSelected == value)
				return;

			_isSelected = value;
			OnPropertyChanged();
		}
	}

	private bool _isBreak;

	/// <summary>
	/// Is socket has break.
	/// </summary>
	public bool IsBreak
	{
		get => _isBreak;
		set
		{
			if (_isBreak == value)
				return;

			_isBreak = value;
			OnPropertyChanged();
		}
	}

	private bool _isBreakActive;

	/// <summary>
	/// Is socket break active.
	/// </summary>
	public bool IsBreakActive
	{
		get => _isBreakActive;
		set
		{
			if (_isBreakActive == value)
				return;

			_isBreakActive = value;
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// Available input data types.
	/// </summary>
	public IList<DiagramSocketType> AvailableTypes { get; private set; }

	/// <summary>
	/// Socket action.
	/// </summary>
	public Action<DiagramSocketValue> Action { get; set; }

	/// <summary>
	/// The event of the socket connection with another one.
	/// </summary>
	public event Action<DiagramSocket, DiagramSocket> Connected;

	/// <summary>
	/// The socket disconnection event.
	/// </summary>
	public event Action<DiagramSocket, DiagramSocket> Disconnected;

	/// <summary>
	/// Initializes a new instance of the <see cref="DiagramSocket"/>.
	/// </summary>
	public DiagramSocket(DiagramSocketDirection dir, string socketId)
	{
		if(socketId != null && socketId.IsEmptyOrWhiteSpace())
			throw new ArgumentException(LocalizedStrings.InvalidValue, nameof(socketId));

		GuiWrapper = new DispatcherNotifiableObject<DiagramSocket>(ConfigManager.GetService<IDispatcher>(), this);

		Id = socketId ?? Guid.NewGuid().ToN();
		Directon = dir;
		AvailableTypes = [];
		this.ResetAvailableTypes();
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		GuiWrapper.DoDispose();
		base.DisposeManaged();
	}

	/// <summary>
	/// Extra validation for the ability to make a connection.
	/// </summary>
	public event Func<DiagramSocket, bool> CanConnectEx;

	/// <summary>
	/// To check the ability to make a connection.
	/// </summary>
	/// <param name="to">Connection.</param>
	/// <returns>The test result.</returns>
	public bool CanConnect(DiagramSocket to)
	{
		if (to == null)
			throw new ArgumentNullException(nameof(to));

		bool isTrue() => to.CanConnectEx?.Invoke(this) ?? true;

		if (to.Type.Type.IsAssignableFrom(Type.Type) && to.Type != DiagramSocketType.Any)
			return isTrue();

		if (to.AvailableTypes.Contains(DiagramSocketType.Any) && to.Type == DiagramSocketType.Any)
			return to.CanConnectFrom(this);

		if (to.AvailableTypes.Contains(Type))
			return isTrue();

		return false;
	}

	/// <summary>
	/// To check the ability to make a connection.
	/// </summary>
	/// <param name="from">Connection.</param>
	/// <returns>The test result.</returns>
	public virtual bool CanConnectFrom(DiagramSocket from) => true;

	/// <summary>
	/// Is input.
	/// </summary>
	public bool IsInput => Directon == DiagramSocketDirection.In;

	/// <summary>
	/// Is output.
	/// </summary>
	public bool IsOutput => Directon == DiagramSocketDirection.Out;

	private readonly HashSet<DiagramSocket> _connections = [];

	/// <summary>
	/// Is socket has connections.
	/// </summary>
	public bool IsConnected => _connections.Any();

	/// <summary>
	/// Invoke <see cref="Connected"/> event.
	/// </summary>
	/// <param name="other"><see cref="DiagramSocket"/></param>
	public void Connect(DiagramSocket other)
	{
		if (other.Directon == Directon)
			throw new InvalidOperationException("invalid direction");

		_connections.Add(other);
		Connected?.Invoke(this, other);
	}

	/// <summary>
	/// Invoke <see cref="Connected"/> event.
	/// </summary>
	/// <param name="other"><see cref="DiagramSocket"/></param>
	public void Disconnect(DiagramSocket other)
	{
		_connections.Remove(other);
		Disconnected?.Invoke(this, other);
	}

	#region INotifyPropertyChanged

	/// <summary>
	/// The connection properties value change event.
	/// </summary>
	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// To call the connection property value change event.
	/// </summary>
	/// <param name="propertyName">Property name.</param>
	private void OnPropertyChanged([CallerMemberName]string propertyName = default)
		=> PropertyChanged?.Invoke(this, propertyName);

	#endregion

	/// <inheritdoc />
	public override string ToString()
	{
		return Parent != null
			? $"{Parent.Name} ({Name})"
			: $"{Name} ({Id})";
	}
}
