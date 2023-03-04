namespace StockSharp.Algo.Compilation;

using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;
using Ecng.Collections;

/// <summary>
/// Code info.
/// </summary>
public class CodeInfo : IPersistable
{
	/// <summary>
	/// Code.
	/// </summary>
	public string Text { get; set; }

	private readonly CachedSynchronizedSet<CodeReference> _references = new(CodeExtensions.DefaultReferences);

	/// <summary>
	/// References.
	/// </summary>
	public INotifyList<CodeReference> References => _references;

	void IPersistable.Load(SettingsStorage storage)
	{
		Text = storage.GetValue(nameof(Text), Text);

		_references.Clear();
		_references.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(References)).Select(s => s.Load<CodeReference>()).ToArray());
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Text), Text);
		storage.SetValue(nameof(References), _references.Cache.Select(r => r.Save()).ToArray());
	}
}