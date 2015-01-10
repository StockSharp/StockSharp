namespace StockSharp.Hydra.Core
{
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Localized task settings display name.
	/// </summary>
	public class TaskDisplayNameAttribute : DisplayNameAttribute
	{
		const string _sourceAlor = "Alor (история)";
		const string _sourceOandaHist = "OANDA (история)";
		const string _sourceCompetition = "ЛЧИ";
		const string _sourceUxWeb = "UX (сайт)";

		private static readonly Dictionary<string, string> _sourceNames = new Dictionary<string, string>
		{
			{ _sourceAlor,         LocalizedStrings.AlorHistory },
			{ _sourceOandaHist,    LocalizedStrings.OandaHistory },
			{ _sourceCompetition,  LocalizedStrings.Str2825 },
			{ _sourceUxWeb,        LocalizedStrings.Str2830 },
		};

		internal static string GetSourceName(string key)
		{
			string name;
			return _sourceNames.TryGetValue(key, out name) ? name : key;
		}

		/// <summary>
		/// Initizalize new instance.
		/// </summary>
		/// <param name="sourceName">Default name of the task.</param>
		public TaskDisplayNameAttribute(string sourceName) 
			: base(GetSourceName(sourceName)) {}
	}

	/// <summary>
	/// Localized task settings display name.
	/// </summary>
	public class TaskSettingsDisplayNameAttribute : DisplayNameAttribute
	{
		/// <summary>
		/// Initizalize new instance.
		/// </summary>
		/// <param name="sourceName">Default name of the task.</param>
		/// <param name="isKey"></param>
		public TaskSettingsDisplayNameAttribute(string sourceName, bool isKey = false)
			: base(isKey ? LocalizedStrings.GetString(sourceName) : LocalizedStrings.TaskSettings.Put(TaskDisplayNameAttribute.GetSourceName(sourceName))) { }
	}

	/// <summary>
	/// Localized task settings category name.
	/// </summary>
	public class TaskCategoryAttribute : CategoryAttribute
	{
		/// <summary>
		/// Initizalize new instance.
		/// </summary>
		/// <param name="sourceName">Default name of the task.</param>
		public TaskCategoryAttribute(string sourceName) 
			: base(TaskDisplayNameAttribute.GetSourceName(sourceName)) {}
	}
}
