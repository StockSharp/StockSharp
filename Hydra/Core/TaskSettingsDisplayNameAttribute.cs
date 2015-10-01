namespace StockSharp.Hydra.Core
{
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
}