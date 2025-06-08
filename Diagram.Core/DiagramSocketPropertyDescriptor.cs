namespace StockSharp.Diagram;

class DiagramSocketPropertyDescriptor(string category, DiagramSocket socket)
	: NamedPropertyDescriptor(socket.Id + socket.GetHashCode(), [])
{
	private readonly DiagramSocket _socket = socket;

	public override bool CanResetValue(object component)
	{
		return false;
	}

	public override object GetValue(object component)
	{
		return _socket.Value;
	}

	public override void ResetValue(object component)
	{
	}

	public override void SetValue(object component, object value)
	{
	}

	public override bool ShouldSerializeValue(object component)
	{
		return false;
	}

	public override Type ComponentType => _socket.Type.Type;

	public override bool IsReadOnly => true;

	public override Type PropertyType => _socket.Type.Type;

	public override string Category { get; } = category ?? throw new ArgumentNullException(nameof(category));

	public override string DisplayName => _socket.Name;

	public override string Description => _socket.Name;
}