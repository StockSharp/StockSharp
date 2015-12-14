#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: TempFiles.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
