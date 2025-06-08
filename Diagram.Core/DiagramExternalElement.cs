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

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		var paramsStorage = new SettingsStorage();

		foreach (var param in Parameters)
		{
			if (!param.IgnoreOnSave)
				paramsStorage.Set(param.Name, param.Save());
		}

		storage.Set(nameof(Parameters), paramsStorage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		var paramsStorage = storage.GetValue<SettingsStorage>(nameof(Parameters));

		if (paramsStorage == null)
			return;

		foreach (var param in Parameters)
		{
			if (!param.IgnoreOnSave)
				param.LoadIfNotNull(paramsStorage, param.Name);
		}
	}
}