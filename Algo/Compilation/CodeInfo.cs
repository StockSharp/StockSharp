namespace StockSharp.Algo.Compilation;

using Ecng.Compilation;
using Ecng.Reflection;

using Nito.AsyncEx;

using StockSharp.Configuration;

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
		_assemblyReferences.Changed += OnReferencesChanged;
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
		_assemblyReferences.Changed -= OnReferencesChanged;

		if (_ownContext)
			Context.Dispose();

		GC.SuppressFinalize(this);
	}

	private void OnReferencesChanged()
		=> NotifyChanged(nameof(AssemblyReferences));

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

	/// <summary>
	/// Code language.
	/// </summary>
	public string Language { get; set; } = FileExts.CSharp;

	private readonly CachedSynchronizedSet<AssemblyReference> _assemblyReferences = new(CodeExtensions.DefaultReferences);

	/// <summary>
	/// Assembly references.
	/// </summary>
	public IList<AssemblyReference> AssemblyReferences => _assemblyReferences;

	private readonly CachedSynchronizedSet<ICodeReference> _projectReferences = [];

	/// <summary>
	/// File references.
	/// </summary>
	public IList<ICodeReference> ProjectReferences => _projectReferences;

	private readonly CachedSynchronizedSet<NuGetReference> _nugetReferences = [];

	/// <summary>
	/// NuGet references.
	/// </summary>
	public IList<NuGetReference> NuGetReferences => _nugetReferences;

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
		=> AsyncContext.Run(() => CompileAsync(isTypeCompatible, typeName, default));

	/// <summary>
	/// Compile code.
	/// </summary>
	/// <param name="isTypeCompatible">Is type compatible.</param>
	/// <param name="typeName">Type name.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="CompilationResult"/></returns>
	public async Task<CompilationResult> CompileAsync(Func<Type, bool> isTypeCompatible, string typeName, CancellationToken cancellationToken)
	{
		IsCompilable = false;

		var sources = new[] { Text };

		if (ExtraSources is not null)
			sources = sources.Concat(ExtraSources);

		(string name, byte[] body)[] refs = null;

		try
		{
			refs = (await _assemblyReferences.Cache.Concat(_projectReferences.Cache).Concat(_nugetReferences.Cache).ToValidRefImages(cancellationToken)).ToArray();
		}
		catch (Exception ex)
		{
			ex.LogError();

			return new()
			{
				Errors = [new CompilationError
				{
					Message = ex.Message,
					Type = CompilationErrorTypes.Error,
				}],
			};
		}

		byte[] asm = null;
		var errors = new List<CompilationError>();

		var cache = ServicesRegistry.TryCompilerCache;

		if (cache?.TryGet(Language, sources, refs.Select(r => r.name), out asm) != true)
		{
			var result = await Language.GetCompiler().Compile("Strategy", sources, refs, cancellationToken);

			if (result.HasErrors())
				return result;

			errors.AddRange(result.Errors);

			try
			{
				cache?.Add(Language, sources, refs.Select(r => r.name), asm = result.Assembly);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
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
		Language = storage.GetValue(nameof(Language), Language);
		Text = storage.GetValue(nameof(Text), storage.GetValue<string>("SourceCode"))?.Replace("ChartIndicatorDrawStyles", "DrawStyles");
		ExtraSources = storage.GetValue(nameof(ExtraSources), ExtraSources);

		_assemblyReferences.Clear();
		_assemblyReferences.AddRange((storage.GetValue<IEnumerable<SettingsStorage>>(nameof(AssemblyReferences)) ?? storage.GetValue<IEnumerable<SettingsStorage>>("References")).Select(s => s.Load<AssemblyReference>()));

		_nugetReferences.Clear();

		if (storage.ContainsKey(nameof(NuGetReferences)))
			_nugetReferences.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(NuGetReferences)).Select(s => s.Load<NuGetReference>()));

		_projectReferences.Clear();

		if (storage.ContainsKey(nameof(ProjectReferences)))
		{
			foreach (var projId in storage.GetValue<IEnumerable<string>>(nameof(ProjectReferences)))
			{
				if (CodeExtensions.TryGetProjectReference(projId, out var projRef))
					_projectReferences.Add(projRef);
			}
		}
	}

	/// <inheritdoc />
	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Id), Id)
			.Set(nameof(Name), Name)
			.Set(nameof(Language), Language)
			.Set(nameof(ExtraSources), ExtraSources)
			.Set(nameof(Text), Text)
			.Set(nameof(AssemblyReferences), _assemblyReferences.Cache.Select(r => r.Save()).ToArray())
			.Set(nameof(NuGetReferences), _nugetReferences.Cache.Select(r => r.Save()).ToArray())
			.Set(nameof(ProjectReferences), _projectReferences.Cache.Select(r => r.Id).ToArray())
		;
	}
}