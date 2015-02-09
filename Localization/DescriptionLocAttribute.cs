namespace StockSharp.Localization
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	/// <summary>
	/// Specifies a description for a property or event.
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public class DescriptionLocAttribute : DescriptionAttribute
	{
		/// <summary>
		/// Initializes a new instance of DescriptionLocAttribute class with description.
		/// </summary>
		/// <param name="resourceId">Id of string resource.</param>
		/// <param name="appendDot">Append dot character ('.') at the end of description. Ignore by default.</param>
		public DescriptionLocAttribute(string resourceId, bool appendDot = false)
			: base(GetDescription(resourceId, appendDot))
		{
		}

		private static string GetDescription(string resourceId, bool appendDot)
		{
			var desc = LocalizedStrings.GetString(resourceId);

			if (appendDot && desc.Last() != '.')
				desc += '.';

			return desc;
		}
	}
}