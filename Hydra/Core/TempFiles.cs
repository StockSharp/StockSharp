namespace StockSharp.Hydra.Core
{
	using StockSharp.Localization;

	/// <summary>
	/// Использование временных файлов.
	/// </summary>
	public enum TempFiles
	{
		/// <summary>
		/// Не использовать.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.NotUseKey)]
		NotUse,

		/// <summary>
		/// Использовать.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.UseKey)]
		Use,

		/// <summary>
		/// Использовать и удалять.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.UseAndDeleteKey)]
		UseAndDelete,
	}
}
