namespace StockSharp.Algo.Compilation;

using Ecng.Compilation;

using Nito.AsyncEx;

/// <summary>
/// Code info.
/// </summary>
public class CodeInfo : NotifiableObject, IPersistable, IDisposable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CodeInfo"/>.
	/// </summary>
	public CodeInfo()
	{
		_assemblyReferences.Changed += OnReferencesChanged;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_assemblyReferences.Changed -= OnReferencesChanged;

		Context?.DoDispose();

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

	private string _language = FileExts.CSharp;

	/// <summary>
	/// Code language.
	/// </summary>
	public string Language
	{
		get => _language;
		set
		{
			if (_language.EqualsIgnoreCase(value))
				return;

			_language = value.ThrowIfEmpty(nameof(value));

			if (Context is not null)
				throw new InvalidOperationException("Language cannot be changed after compilation.");

			if (value.EqualsIgnoreCase(FileExts.FSharp))
				_assemblyReferences.AddRange(CodeExtensions.FSharpReferences);
		}
	}

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
	public IType ObjectType { get; private set; }

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
	public object Context { get; private set; }

	/// <summary>
	/// Last built assembly.
	/// </summary>
	public IAssembly Assembly { get; private set; }

	/// <summary>
	/// Compile code.
	/// </summary>
	/// <param name="isTypeCompatible">Is type compatible.</param>
	/// <param name="typeName">Type name.</param>
	/// <returns><see cref="CompilationResult"/></returns>
	public IEnumerable<CompilationError> Compile(Func<IType, bool> isTypeCompatible = default, string typeName = default)
		=> AsyncContext.Run(() => CompileAsync(isTypeCompatible, typeName, default));

	/// <summary>
	/// Compile code.
	/// </summary>
	/// <param name="isTypeCompatible">Is type compatible.</param>
	/// <param name="typeName">Type name.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="CompilationResult"/></returns>
	public async Task<IEnumerable<CompilationError>> CompileAsync(Func<IType, bool> isTypeCompatible, string typeName, CancellationToken cancellationToken)
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

			return [new CompilationError
			{
				Message = ex.Message,
				Type = CompilationErrorTypes.Error,
			}];
		}

		IAssembly asm = null;
		var errors = new List<CompilationError>();

		var compiler = Language.GetCompiler();

		Context ??= compiler.CreateContext();

		var cache = compiler.IsAssembly ? ServicesRegistry.TryCompilerCache : null;

		if (cache?.TryGet(Language, sources, refs.Select(r => r.name), out asm) != true)
		{
			var result = await compiler.Compile("Strategy", sources, refs, cancellationToken);

			if (result.HasErrors())
				return result.Errors;

			errors.AddRange(result.Errors);

			asm = result.Assembly;

			try
			{
				cache?.Add(Language, sources, refs.Select(r => r.name), asm);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		IsCompilable = true;

		if (asm is not null)
		{
			Assembly = asm;

			try
			{
				ObjectType = asm.GetExportTypes(Context).TryFindType(isTypeCompatible, typeName);
			}
			catch (Exception ex)
			{
				errors.Add(new()
				{
					Message = ex.Message,
					Type = CompilationErrorTypes.Error,
				});
			}

			try
			{
				Compiled?.Invoke();
			}
			catch (Exception ex)
			{
				errors.Add(new()
				{
					Message = ex.Message,
					Type = CompilationErrorTypes.Error,
				});
			}
		}

		return errors;
	}

	/// <inheritdoc />
	public void Load(SettingsStorage storage)
	{
		Id = storage.GetValue(nameof(Id), Id);
		Name = storage.GetValue(nameof(Name), Name);
		Language = storage.GetValue(nameof(Language), Language).ReplaceIgnoreCase("csharp", FileExts.CSharp);
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