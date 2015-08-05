namespace StockSharp.Hydra.Core
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Localized task settings display name.
	/// </summary>
	public class TaskSettingsDisplayNameAttribute : DisplayNameLocAttribute
	{
		/// <summary>
		/// Initizalize new instance.
		/// </summary>
		/// <param name="sourceName">Default name of the task.</param>
		public TaskSettingsDisplayNameAttribute(string sourceName)
			: base(LocalizedStrings.TaskSettings, sourceName)
		{
		}
	}

	/// <summary>
	/// Документация задачи.
	/// </summary>
	public class TaskDocAttribute : Attribute
	{
		/// <summary>
		/// Ссылка на раздел документации.
		/// </summary>
		public string DocUrl { get; private set; }

		/// <summary>
		/// Создать <see cref="TaskDocAttribute"/>.
		/// </summary>
		/// <param name="docUrl">Ссылка на раздел документации.</param>
		public TaskDocAttribute(string docUrl)
		{
			if (docUrl.IsEmpty())
				throw new ArgumentNullException("docUrl");

			DocUrl = docUrl;
		}
	}

	/// <summary>
	/// Иконка задачи.
	/// </summary>
	public class TaskIconAttribute : Attribute
	{
		/// <summary>
		/// Иконка.
		/// </summary>
		public string Icon { get; private set; }

		/// <summary>
		/// Создать <see cref="TaskIconAttribute"/>.
		/// </summary>
		/// <param name="icon">Иконка.</param>
		public TaskIconAttribute(string icon)
		{
			if (icon.IsEmpty())
				throw new ArgumentNullException("icon");

			Icon = icon;
		}
	}
}