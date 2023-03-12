namespace StockSharp.Algo.Compilation;

using System;
using System.Linq;
using System.Runtime.Loader;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;
using Ecng.Compilation;
using Ecng.Collections;

using StockSharp.Logging;
using StockSharp.Localization;
using StockSharp.Algo;

/// <summary>
/// Code info.
/// </summary>
public class CodeInfo : IPersistable
{
	/// <summary>
	/// Identifier.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// <see cref="Name"/> changed event.
	/// </summary>
	public event Action<CodeInfo> NameChanged;

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
			NameChanged?.Invoke(this);
		}
	}

	/// <summary>
	/// Code.
	/// </summary>
	public string Text { get; set; }

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
	/// Compiled event.
	/// </summary>
	public event Action Compiled;

	private AssemblyLoadContext _context;

	/// <summary>
	/// Compile code.
	/// </summary>
	/// <param name="isTypeCompatible">Is type compatible.</param>
	/// <returns><see cref="CompilationResult"/></returns>
	public CompilationResult Compile(Func<Type, bool> isTypeCompatible)
	{
		if (isTypeCompatible is null)
			throw new ArgumentNullException(nameof(isTypeCompatible));

		var prev = _context;

		_context = new(default, true);
		var result = ServicesRegistry.Compiler.CompileCode(_context, Text, string.Empty, References);

		if (result.HasErrors())
			return result;

		var type = result.Assembly.GetTypes().FirstOrDefault(isTypeCompatible);

		ObjectType = type ?? throw new InvalidOperationException(LocalizedStrings.Str3608);

		try
		{
			Compiled?.Invoke();
		}
		catch (Exception ex)
		{
			ex.LogError();
		}

		try
		{
			prev?.Unload();
		}
		catch (Exception ex)
		{
			ex.LogError();
		}

		return result;
	}

	/// <summary>
	/// 
	/// </summary>
	public string Key => $"_{Id:N}";

	void IPersistable.Load(SettingsStorage storage)
	{
		Id = storage.GetValue(nameof(Id), Id);
		Name = storage.GetValue(nameof(Name), Name);
		Text = storage.GetValue(nameof(Text), storage.GetValue<string>("SourceCode"));

		_references.Clear();
		_references.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(References)).Select(s => s.Load<CodeReference>()).ToArray());
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Id), Id);
		storage.SetValue(nameof(Name), Name);
		storage.SetValue(nameof(Text), Text);
		storage.SetValue(nameof(References), _references.Cache.Select(r => r.Save()).ToArray());
	}
}