namespace StockSharp.Diagram.Elements;

/// <summary>
/// The diagram element with the changeable data type.
/// </summary>
/// <typeparam name="T">Type of element.</typeparam>
public abstract class TypedDiagramElement<T> : DiagramElement
	where T : TypedDiagramElement<T>
{
	/// <summary>
	/// </summary>
	protected sealed class SocketTypesSource : ItemsSourceBase<DiagramSocketType>
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly List<DiagramSocketType> _collection = [];

		/// <summary>
		/// </summary>
		public static void SetValues(IEnumerable<DiagramSocketType> sockets)
		{
			_collection.Clear();
			_collection.AddRange(sockets);
		}

		/// <inheritdoc />
		protected override string GetName(DiagramSocketType st) => st.Name;

		/// <inheritdoc />
		protected override IEnumerable<DiagramSocketType> GetValues() => _collection;
	}

	private readonly DiagramElementParam<DiagramSocketType> _type;

	/// <summary>
	/// Data type.
	/// </summary>
	public DiagramSocketType Type
	{
		get => _type.Value;
		set => _type.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TypedDiagramElement{T}"/>.
	/// </summary>
	/// <param name="typeParamCategory">The category of the diagram element parameter.</param>
	/// <param name="ignoreHandler"></param>
	protected TypedDiagramElement(string typeParamCategory, bool ignoreHandler = false)
	{
		var inputSocket = AddInput(StaticSocketIds.Input, LocalizedStrings.Input, DiagramSocketType.Any, ignoreHandler ? null : OnProcess, int.MaxValue);
		var outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Output, DiagramSocketType.Any);

		_type = AddParam(nameof(Type), DiagramSocketType.Any)
			.SetBasic(true)
			.SetDisplay(typeParamCategory, LocalizedStrings.Type, LocalizedStrings.DataTypeDesc, 10)
			.SetEditor(new ItemsSourceAttribute(typeof(SocketTypesSource)))
			.SetOnValueChangedHandler(value =>
			{
				value ??= DiagramSocketType.Any;

				SetElementName(value.ToString());

				var socket = inputSocket;

				if (value == DiagramSocketType.Unit)
					socket.AllowConvertToNumeric();
				else
					socket.ResetAvailableTypes();

				if (socket.Type == value)
					return;

				socket.Type = value;

				RaiseSocketChanged(socket);
				TypeChanged();
				RaisePropertiesChanged();
			})
			.SetSaveLoadHandlers(SaveType, LoadType);

		inputSocket.Connected += InputSocketConnected;
		inputSocket.Disconnected += InputSocketDisconnected;
		outputSocket.Connected += OutputSocketConnected;

		OutputSocket = outputSocket;
	}

	/// <summary>
	/// Output socket.
	/// </summary>
	protected DiagramSocket OutputSocket { get; }

	/// <summary>
	/// The method is called at subscription to the processing of diagram element output values.
	/// </summary>
	/// <param name="socket">The diagram element socket.</param>
	/// <param name="source">The source diagram element socket.</param>
	private void InputSocketConnected(DiagramSocket socket, DiagramSocket source)
	{
		SocketConnected(socket, source);
		OnInputSocketConnected(socket, source);
	}

	/// <summary>
	/// The method is called at unsubscription from the processing of diagram element output values.
	/// </summary>
	/// <param name="socket">The diagram element socket.</param>
	/// <param name="source">The source diagram element socket.</param>
	private void InputSocketDisconnected(DiagramSocket socket, DiagramSocket source)
	{
		var type = GetConnectedSourceSockets(socket).FirstOrDefault()?.Type ?? DiagramSocketType.Any;

		if (type != Type)
		{
			Type = type;
			TypeChanged();
		}

		RaisePropertiesChanged();
		OnInputSocketDisconnected(socket, source);
	}

	private void OutputSocketConnected(DiagramSocket socket, DiagramSocket from)
	{
		SocketConnected(socket, from);
	}

	private void SocketConnected(DiagramSocket socket, DiagramSocket from)
	{
		if (Type != null && Type != DiagramSocketType.Any)
			return;

		if (socket.Type != from.Type)
			socket.Type = from.Type;

		Type = from.Type;

		TypeChanged();
		RaisePropertiesChanged();
	}

	/// <summary>
	/// The method is called when the data type is changed.
	/// </summary>
	protected virtual void TypeChanged()
	{
	}

	/// <summary>
	/// The method is called when the input socket is connected.
	/// </summary>
	/// <param name="socket">The diagram element socket.</param>
	/// <param name="source">The source diagram element socket.</param>
	protected virtual void OnInputSocketConnected(DiagramSocket socket, DiagramSocket source)
	{
	}

	/// <summary>
	/// The method is called when the input socket is disconnected.
	/// </summary>
	/// <param name="socket">The diagram element socket.</param>
	/// <param name="source">The source diagram element socket.</param>
	protected virtual void OnInputSocketDisconnected(DiagramSocket socket, DiagramSocket source)
	{
	}

	/// <summary>
	/// The method is called at the processing of the new incoming value.
	/// </summary>
	/// <param name="value">The processed value.</param>
	protected abstract void OnProcess(DiagramSocketValue value);

	/// <summary>
	/// To change the output socket type.
	/// </summary>
	protected void UpdateOutputSocketType()
	{
		var output = OutputSockets.First();

		if (output.Type == Type)
			return;

		//var raiseChanged = output.Type != DiagramSocketType.Any;

		output.Type = Type;

		//if (raiseChanged)
		RaiseSocketChanged(output);
	}

	private static SettingsStorage SaveType(DiagramSocketType type)
	{
		type ??= DiagramSocketType.Any;

		var settings = new SettingsStorage();
		settings.SetValue(nameof(Type), type.Type.GetTypeName(false));
		return settings;
	}

	private static DiagramSocketType LoadType(SettingsStorage settings)
	{
		var type = settings.GetValue<Type>(nameof(Type));

		if (type == null)
			return DiagramSocketType.Any;

		return type.ToDiagramType();
	}
}