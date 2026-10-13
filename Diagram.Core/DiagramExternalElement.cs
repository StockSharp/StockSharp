namespace StockSharp.Diagram;

/// <summary>
/// Element uses external code.
/// </summary>
public abstract class DiagramExternalElement : BaseLogReceiver
{
	private readonly CachedSynchronizedDictionary<string, IDiagramElementParam> _parameters = [];

	/// <summary>
	/// Parameters.
	/// </summary>
	public IEnumerable<IDiagramElementParam> Parameters => _parameters.CachedValues;

	/// <summary>
	/// Initializes a new instance of the <see cref="DiagramExternalElement"/>.
	/// </summary>
	protected DiagramExternalElement()
    {
    }

	/// <summary>
	/// Container.
	/// </summary>
	public DiagramElement Container { get; internal set; }

	/// <summary>
	/// Wait all parameters before invoke method.
	/// </summary>
	public virtual bool WaitAllInput => true;

	/// <summary>
	/// To add a parameter.
	/// </summary>
	/// <typeparam name="T">Parameter type.</typeparam>
	/// <param name="name">Name.</param>
	/// <param name="value">Value.</param>
	/// <returns>Parameter.</returns>
	protected DiagramElementParam<T> AddParam<T>(string name, T value = default)
	{
		if (_parameters.ContainsKey(name))
			throw new ArgumentException($"Parameter '{name}' already exist.");

		var param = new DiagramElementParam<T>
		{
			Name = name,
			Value = value,
		};

		_parameters.Add(name, param);

		return param;
	}

	/// <summary>
	/// <see cref="DiagramElement.Start"/>
	/// </summary>
	public virtual void Start()
	{
	}

	/// <summary>
	/// <see cref="DiagramElement.Stop"/>
	/// </summary>
	public virtual void Stop()
	{
	}

	/// <summary>
	/// <see cref="DiagramElement.Reset"/>
	/// </summary>
	public virtual void Reset()
	{
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);

		var paramsStorage = new SettingsStorage();

		foreach (var param in Parameters)
		{
			if (!param.IgnoreOnSave)
				paramsStorage.Set(param.Name, await param.SaveAsync(cancellationToken));
		}

		storage.Set(nameof(Parameters), paramsStorage);
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);

		var paramsStorage = storage.GetValue<SettingsStorage>(nameof(Parameters));

		if (paramsStorage == null)
			return;

		foreach (var param in Parameters)
		{
			if (!param.IgnoreOnSave && paramsStorage.GetValue<SettingsStorage>(param.Name) is { } paramStorage)
				await param.LoadAsync(paramStorage, cancellationToken);
		}
	}
}