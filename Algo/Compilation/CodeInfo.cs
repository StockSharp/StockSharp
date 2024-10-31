namespace StockSharp.Algo.Compilation;

using Ecng.Compilation;

/// <summary>
/// Code info.
/// </summary>
public class CodeInfo : NotifiableObject, IPersistable, IDisposable
{
	private readonly bool _ownContext;

	/// <summary>
	/// Initializes a new instance of the <see cref="CodeInfo"/>.
	/// </summary>
	public CodeInfo()
		: this(new(), true)
    {
		_references.Changed += OnReferencesChanged;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CodeInfo"/>.
	/// </summary>
	/// <param name="context"><see cref="AssemblyLoadContextTracker"/></param>
	/// <param name="ownContext">Own context.</param>
	public CodeInfo(AssemblyLoadContextTracker context, bool ownContext)
	{
		Context = context ?? throw new ArgumentNullException(nameof(context));
		_ownContext = ownContext;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_references.Changed -= OnReferencesChanged;

		if (_ownContext)
			Context.Dispose();

		GC.SuppressFinalize(this);
	}

	private void OnReferencesChanged()
		=> NotifyChanged(nameof(References));

	private Guid _id = Guid.NewGuid();

	/// <summary>
	/// Identifier.
	/// </summary>
	public Guid Id
	{
		get => _id;
		set
		{
			if (_id == value)
				return;

			_id = value;
			NotifyChanged();
		}
	}

	private string _name;

	/// <summary>
	/// Name.
	/// </summary>
	public string Name
	{
		get => _name;
		set
		{
			if (_name == value)
				return;

			_name = value;
			NotifyChanged();
		}
	}

	private string _text;

	/// <summary>
	/// Code.
	/// </summary>
	public string Text
	{
		get => _text;
		set
		{
			if (_text == value)
				return;

			_text = value;
			NotifyChanged();
		}
	}

	private readonly CachedSynchronizedSet<CodeReference> _references = new(CodeExtensions.DefaultReferences);

	/// <summary>
	/// References.
	/// </summary>
	public INotifyList<CodeReference> References => _references;

	/// <summary>
	/// Object type.
	/// </summary>
	public Type ObjectType { get; private set; }

	/// <summary>
	/// The code is compilable.
	/// </summary>
	public bool IsCompilable { get; private set; }

	private string[] _extraSources;

	/// <summary>
	/// Extra source codes.
	/// </summary>
	public string[] ExtraSources
	{
		get => _extraSources;
		set
		{
			if ((_extraSources is null && value is null) || (_extraSources is not null && value is not null && _extraSources.SequenceEqual(value)))
				return;

			_extraSources = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Compiled event.
	/// </summary>
	public event Action Compiled;

	/// <summary>
	/// <see cref="AssemblyLoadContextTracker"/>
	/// </summary>
	public AssemblyLoadContextTracker Context { get; }

	/// <summary>
	/// Last built assembly.
	/// </summary>
	public byte[] Assembly { get; private set; }

	/// <summary>
	/// Compile code.
	/// </summary>
	/// <param name="isTypeCompatible">Is type compatible.</param>
	/// <param name="typeName">Type name.</param>
	/// <returns><see cref="CompilationResult"/></returns>
	public CompilationResult Compile(Func<Type, bool> isTypeCompatible = default, string typeName = default)
	{
		IsCompilable = false;

		var sources = new[] { Text };

		if (ExtraSources is not null)
			sources = sources.Concat(ExtraSources);

		var refs = References.ToValidPaths().ToArray();

		byte[] asm = null;
		var errors = new List<CompilationError>();

		var cache = ServicesRegistry.TryCompilerCache;

		if (cache?.TryGet(sources, refs, out asm) != true)
		{
			var result = ServicesRegistry.Compiler.Compile("Strategy", sources, refs);

			if (result.HasErrors())
				return result;

			errors.AddRange(result.Errors);
			cache?.Add(sources, refs, asm = result.Assembly);
		}

		IsCompilable = true;

		ObjectType = Context.LoadFromStream(asm).TryFindType(isTypeCompatible, typeName);

		try
		{
			Compiled?.Invoke();
		}
		catch (Exception ex)
		{
			ex.LogError();
		}

		Assembly = asm;

		return new()
		{
			Assembly = asm,
			Errors = [.. errors],
		};
	}

	/// <inheritdoc />
	public void Load(SettingsStorage storage)
	{
		Id = storage.GetValue(nameof(Id), Id);
		Name = storage.GetValue(nameof(Name), Name);
		Text = storage.GetValue(nameof(Text), storage.GetValue<string>("SourceCode"))?.Replace("ChartIndicatorDrawStyles", "DrawStyles");
		ExtraSources = storage.GetValue(nameof(ExtraSources), ExtraSources);

		_references.Clear();
		_references.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(References)).Select(s => s.Load<CodeReference>()).ToArray());
	}

	/// <inheritdoc />
	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Id), Id)
			.Set(nameof(Name), Name)
			.Set(nameof(ExtraSources), ExtraSources)
			.Set(nameof(Text), Text)
			.Set(nameof(References), _references.Cache.Select(r => r.Save()).ToArray());
	}
}