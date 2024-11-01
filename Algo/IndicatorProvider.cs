namespace StockSharp.Algo;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Reflection;

using StockSharp.Algo.Compilation;
using StockSharp.Algo.Indicators;

/// <summary>
/// <see cref="IndicatorType"/> provider.
/// </summary>
public class IndicatorProvider : IIndicatorProvider
{
	private readonly CachedSynchronizedSet<IndicatorType> _indicatorTypes = [];
	private readonly SynchronizedSet<IndicatorType> _custom = [];
	private readonly SynchronizedDictionary<Type, Type> _painterTypes = [];
	private readonly CachedSynchronizedDictionary<IndicatorType, AssemblyLoadContextTracker> _asmContexts = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorProvider"/>.
	/// </summary>
	public IndicatorProvider()
	{
	}

	/// <inheritdoc />
	public virtual void Init() => OnInit(new Dictionary<Type, Type>());

	/// <summary>
	/// Initialize provider.
	/// </summary>
	/// <param name="painterTypes">Painter types.</param>
	protected virtual void OnInit(IDictionary<Type, Type> painterTypes)
	{
		if (painterTypes is null)
			throw new ArgumentNullException(nameof(painterTypes));

		_asmContexts.CachedValues.ForEach(c => c.Dispose());

		_indicatorTypes.Clear();
		_painterTypes.Clear();
		_custom.Clear();
		_asmContexts.Clear();

		_painterTypes.AddRange(painterTypes);

		var ns = typeof(IIndicator).Namespace;

		_indicatorTypes.AddRange(typeof(BaseIndicator)
			.Assembly
			.FindImplementations<IIndicator>(showObsolete: true, extraFilter: t => t.Namespace == ns && t.GetConstructor(Type.EmptyTypes) != null && t.GetAttribute<IndicatorHiddenAttribute>() is null)
			.Select(t => new IndicatorType(t, _painterTypes.TryGetValue(t)))
			.OrderBy(t => t.Name));
	}

	/// <inheritdoc />
	public IEnumerable<IndicatorType> All => _indicatorTypes.Cache;

	/// <inheritdoc />
	public void Add(IndicatorType type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		_indicatorTypes.Add(type);
		_custom.Add(type);
	}

	void IIndicatorProvider.Remove(IndicatorType type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		_indicatorTypes.Remove(type);
		_custom.Remove(type);

		if (_asmContexts.TryGetAndRemove(type, out var ctx))
			ctx.Dispose();
	}

	private static class Keys
	{
		public const string Code = nameof(Code);
		public const string Assembly = nameof(Assembly);
		public const string TypeName = nameof(TypeName);
	}

	/// <inheritdoc />
	SettingsStorage IIndicatorProvider.Save(IndicatorType type)
	{
		var (code, asm, typeName) = ToSerializationInfo(type);

		return new SettingsStorage()
			.Set(Keys.Code, code.Save())
			.Set(Keys.Assembly, asm)
			.Set(Keys.TypeName, typeName)
		;
	}

	/// <inheritdoc />
	IndicatorType IIndicatorProvider.Load(string id, SettingsStorage storage)
	{
		var code = storage.GetValue<CodeInfo>(Keys.Code);
		var asm = storage.GetValue<byte[]>(Keys.Assembly);
		var typeName = storage.GetValue<string>(Keys.TypeName);

		var (it, context) = FromSerializationInfo(id, code, asm, typeName);

		Add(it);

		if (context is not null)
			_asmContexts.TryAdd(it, context);

		return it;
	}

	/// <summary>
	/// Convert serialization info to <see cref="IndicatorType"/>.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="code"><see cref="CodeInfo"/></param>
	/// <param name="asm">Assembly.</param>
	/// <param name="typeName">Type name.</param>
	/// <returns><see cref="IndicatorType"/></returns>
	protected virtual (IndicatorType type, AssemblyLoadContextTracker context) FromSerializationInfo(string id, CodeInfo code, byte[] asm, string typeName)
	{
		AssemblyLoadContextTracker context;
		Type type;

		if (code is not null)
		{
			code.Compile(typeName: typeName).ThrowIfErrors();

			context = code.Context;
			type = code.ObjectType;
		}
		else
		{
			context = new();
			type = context.LoadFromStream(asm).TryFindType(null, typeName);
		}

		if (type is null)
		{
			context.Dispose();

			throw new InvalidOperationException($"{LocalizedStrings.TypeNotFoundInAssembly} '{typeName}'");
		}

		return (new(type, _painterTypes.TryGetValue(type)), context);
	}

	/// <summary>
	/// Get serialization info.
	/// </summary>
	/// <param name="type"><see cref="IndicatorType"/></param>
	/// <returns>Serialization info.</returns>
	protected virtual (CodeInfo code, byte[] asm, string typeName) ToSerializationInfo(IndicatorType type)
		=> throw new NotSupportedException();

	bool IIndicatorProvider.IsCustom(IndicatorType type)
		=> _custom.Contains(type);
}