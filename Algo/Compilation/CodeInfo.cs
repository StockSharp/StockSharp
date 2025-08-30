namespace StockSharp.Algo.Compilation;

using Ecng.Compilation;
using Ecng.Reflection;

using Nito.AsyncEx;

/// <summary>
/// Code info.
/// </summary>
public class CodeInfo : NotifiableObject, IPersistable, IDisposable
{
	private ICompilerContext _context;

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
		try
		{
			_assemblyReferences.Changed -= OnReferencesChanged;

			var ctx = _context;

			if (ctx is null)
				return;

			ctx.Dispose();

			_context = null;

			var compiler = Language.TryGetCompiler();

			if (compiler is null)
				return;

			var cache = compiler.IsAssemblyPersistable ? ServicesRegistry.TryCompilerCache : null;

			if (cache is null)
				return;

			cache.Remove(Language, GetSources(), GetRefNames());
		}
		finally
		{
			GC.SuppressFinalize(this);
		}
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

	private string _language = CodeExtensions.DefaultLanguage;

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

			if (_context is not null)
				throw new InvalidOperationException("Language cannot be changed after compilation.");

			_language = value.ThrowIfEmpty(nameof(value));

			if (value.EqualsIgnoreCase(FileExts.FSharp))
				_assemblyReferences.AddRange(CodeExtensions.FSharpReferences);
		}
	}

	private readonly CachedSynchronizedSet<AssemblyReference> _assemblyReferences = [.. CodeExtensions.DefaultReferences];

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

	private string _moduleName = "Strategy";

	/// <summary>
	/// Module name.
	/// </summary>
	public string ModuleName
	{
		get => _moduleName;
		set => _moduleName = value;
	}

	/// <summary>
	/// Compiled event.
	/// </summary>
	public event Action Compiled;

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
	public IEnumerable<CompilationError> Compile(Func<Type, bool> isTypeCompatible = default, string typeName = default)
		=> AsyncContext.Run(() => CompileAsync(isTypeCompatible, typeName, default));

	private IEnumerable<string> GetSources()
	{
		var sources = new[] { Text };

		if (ExtraSources is not null)
			sources = sources.Concat(ExtraSources);

		return sources;
	}

	private string[] GetRefNames()
		=> [.. _assemblyReferences.Cache.Concat(_projectReferences.Cache).Concat(_nugetReferences.Cache).Select(r => r.Name)];

	/// <summary>
	/// Compile code.
	/// </summary>
	/// <param name="isTypeCompatible">Is type compatible.</param>
	/// <param name="typeName">Type name.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="CompilationResult"/></returns>
	public async Task<IEnumerable<CompilationError>> CompileAsync(Func<Type, bool> isTypeCompatible, string typeName, CancellationToken cancellationToken)
	{
		IsCompilable = false;

		var errors = new List<CompilationError>();

		var compiler = Language.GetCompiler();

		_context ??= compiler.CreateContext();

		var cache = compiler.IsAssemblyPersistable ? ServicesRegistry.TryCompilerCache : null;

		var sources = GetSources();
		var refNames = GetRefNames();

		Assembly asm = null;
		byte[] asmBody = null;

		if (cache?.TryGet(Language, sources, refNames, out asmBody) == true)
		{
			try
			{
				asm = _context.LoadFromBinary(asmBody);
			}
			catch (Exception ex)
			{
				ex.LogError();

				cache.Remove(Language, sources, refNames);
			}
		}

		if (asm is null)
		{
			(string name, byte[] body)[] refs;

			try
			{
				refs = [.. (await _assemblyReferences.Cache.Concat(_projectReferences.Cache).Concat(_nugetReferences.Cache).ToValidRefImages(cancellationToken))];
			}
			catch (Exception ex)
			{
				ex.LogError();

				return [ex.ToError()];
			}

			var result = await compiler.Compile(ModuleName, sources, refs, cancellationToken);

			if (result.HasErrors())
				return result.Errors;

			errors.AddRange(result.Errors);

			if (result is AssemblyCompilationResult asmRes)
				asmBody = asmRes.AssemblyBody;
			else
				asmBody = null;

			try
			{
				asm = result.GetAssembly(_context);
			}
			catch (Exception ex)
			{
				errors.Add(ex.ToError());
			}

			try
			{
				cache?.Add(Language, sources, refNames, asmBody);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		if (asm is not null)
		{
			IsCompilable = true;
			Assembly = asmBody;

			try
			{
				ObjectType = asm.GetExportedTypes().TryFindType(isTypeCompatible, typeName);
			}
			catch (Exception ex)
			{
				errors.Add(ex.ToError());
			}

			try
			{
				Compiled?.Invoke();
			}
			catch (Exception ex)
			{
				errors.Add(ex.ToError());
			}
		}

		return errors;
	}

	/// <inheritdoc />
	public void Load(SettingsStorage storage)
	{
		Id = storage.GetValue(nameof(Id), Id);
		Name = storage.GetValue(nameof(Name), Name);
		Language = storage.GetValue(nameof(Language), Language);
		Text = (storage.GetValue(nameof(Text), storage.GetValue<string>("SourceCode"))?.Replace("ChartIndicatorDrawStyles", "DrawStyles"))?.SkipBom();
		ExtraSources = storage.GetValue(nameof(ExtraSources), ExtraSources);
		ModuleName = storage.GetValue(nameof(ModuleName), ModuleName);

		_assemblyReferences.Clear();

		var asmRefs = storage.GetValue<IEnumerable<SettingsStorage>>(nameof(AssemblyReferences)) ?? storage.GetValue<IEnumerable<SettingsStorage>>("References");
		if (asmRefs is not null)
			_assemblyReferences.AddRange(asmRefs.Select(s => s.Load<AssemblyReference>()));

		// TODO 2025-02-04 Remove 1 year later
		var oldLogging = _assemblyReferences.Cache.FirstOrDefault(r => r.FileName.EqualsIgnoreCase("StockSharp.Logging.dll"));
		if (oldLogging is not null)
		{
			_assemblyReferences.Remove(oldLogging);
			_assemblyReferences.Add(new() { FileName = "Ecng.Logging.dll" });
		}

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
			.Set(nameof(ModuleName), ModuleName)
			.Set(nameof(AssemblyReferences), _assemblyReferences.Cache.Select(r => r.Save()).ToArray())
			.Set(nameof(NuGetReferences), _nugetReferences.Cache.Select(r => r.Save()).ToArray())
			.Set(nameof(ProjectReferences), _projectReferences.Cache.Select(r => r.Id).ToArray())
		;
	}
}