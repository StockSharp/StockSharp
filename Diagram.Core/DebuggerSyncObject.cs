namespace StockSharp.Diagram;

using Ecng.Configuration;

/// <summary>
/// The synchronization object for the composite elements debugger.
/// </summary>
public class DebuggerSyncObject : ViewModelBase
{
	private readonly Func<DiagramSocket, bool> _isBreak;
	private readonly Action<DebuggerSyncObject> _breakAction;
	private readonly Action<DebuggerSyncObject> _errorAction;
	private readonly SyncObject _inputSync = new();
	private readonly DiagramDebugger _debugger;
	private readonly CompositionDiagramElement _rootElement;

	private DiagramElement _currentElement;
	private bool _waitOnNext;
	private DiagramSocket _currentSocket;
	private Exception _currentError;

	private class DebuggerSyncObjectGuiWrapper(DebuggerSyncObject obj) : DispatcherNotifiableObject<DebuggerSyncObject>(ConfigManager.GetService<IDispatcher>(), obj)
	{
	}

	/// <summary>
	/// Gui wrapper for property binding.
	/// </summary>
	public INotifyPropertyChanged GuiWrapper {get;}

	/// <summary>
	/// The current element.
	/// </summary>
	public DiagramElement CurrentElement
	{
		get => _currentElement;
		private set => SetField(ref _currentElement, value);
	}

	/// <summary>
	/// The current socket.
	/// </summary>
	public DiagramSocket CurrentSocket
	{
		get => _currentSocket;
		private set => SetField(ref _currentSocket, value);
	}

	/// <summary>
	/// The current error.
	/// </summary>
	public Exception CurrentError
	{
		get => _currentError;
		private set => SetField(ref _currentError, value);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DebuggerSyncObject"/>.
	/// </summary>
	/// <param name="debugger"><see cref="DiagramDebugger"/></param>
	/// <param name="rootElement">The root diagram element.</param>
	/// <param name="isBreak">The handler that returns a stop flag for the socket.</param>
	/// <param name="breakAction">The action with the element at stop.</param>
	/// <param name="errorAction">The action with the element at error.</param>
	public DebuggerSyncObject(DiagramDebugger debugger, CompositionDiagramElement rootElement, Func<DiagramSocket, bool> isBreak, Action<DebuggerSyncObject> breakAction, Action<DebuggerSyncObject> errorAction)
	{
		_debugger = debugger ?? throw new ArgumentNullException(nameof(debugger));
		_rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
		_isBreak = isBreak ?? throw new ArgumentNullException(nameof(isBreak));
		_breakAction = breakAction ?? throw new ArgumentNullException(nameof(breakAction));
		_errorAction = errorAction ?? throw new ArgumentNullException(nameof(errorAction));

		GuiWrapper = new DebuggerSyncObjectGuiWrapper(this);
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		GuiWrapper.DoDispose();
		base.DisposeManaged();
	}

	/// <summary>
	/// <see langword="true" />, if the debugger is stopped at the entry of the diagram element. Otherwise, <see langword="false" />.
	/// </summary>
	public bool IsWaitingOnInput { get; private set; }

	/// <summary>
	/// <see langword="true" />, if the debugger is stopped at the exit of the diagram element. Otherwise, <see langword="false" />.
	/// </summary>
	public bool IsWaitingOnOutput { get; private set; }

	/// <summary>
	/// Try wait on socket.
	/// </summary>
	/// <param name="socket"><see cref="DiagramSocket"/></param>
	/// <param name="isOnInput">Is wait on input.</param>
	/// <returns>Operation result.</returns>
	public bool TryWait(DiagramSocket socket, bool isOnInput)
	{
		if (_debugger.IsDisabled)
			return false;

		if (!_isBreak(socket) && !_waitOnNext)
		{
			if (socket.Parent == CurrentElement)
				CurrentElement = null;

			return false;
		}

		try
		{
			socket.IsBreakActive = true;
			CurrentSocket = socket;
			CurrentElement = socket.Parent;

			_breakAction?.Invoke(this);

			SetIsWaiting(isOnInput, true);

			_waitOnNext = false;
			_inputSync.Wait();
		}
		finally
		{
			socket.IsBreakActive = false;
			CurrentSocket = null;
			CurrentElement = null;
		}

		SetIsWaiting(isOnInput, false);

		return true;
	}

	/// <summary>
	/// Try wait on error.
	/// </summary>
	/// <param name="element"><see cref="DiagramElement"/></param>
	/// <param name="error">Error.</param>
	/// <returns>Operation result.</returns>
	public bool TryWaitOnError(DiagramElement element, Exception error)
	{
		if (element is null)
			throw new ArgumentNullException(nameof(element));

		if (error is null)
			throw new ArgumentNullException(nameof(error));

		if (_debugger.IsDisabled)
			return false;

		if (element == _rootElement)
			return false;

		CurrentError = error;
		CurrentElement = element;

		_errorAction?.Invoke(this);

		SetIsWaiting(true, true);

		_waitOnNext = false;
		_inputSync.Wait();

		CurrentError = null;
		CurrentElement = null;

		SetIsWaiting(true, false);

		return true;
	}

	private void SetIsWaiting(bool isOnInput, bool value)
	{
		if (isOnInput)
			IsWaitingOnInput = value;
		else
			IsWaitingOnOutput = value;
	}

	/// <summary>
	/// To set the flag for waiting at the entry of the next diagram element.
	/// </summary>
	public void SetWaitOnNext()
	{
		_waitOnNext = true;
	}

	/// <summary>
	/// Continue.
	/// </summary>
	public void Continue()
	{
		if (IsWaitingOnInput || IsWaitingOnOutput)
			_inputSync.Pulse();
	}
}
