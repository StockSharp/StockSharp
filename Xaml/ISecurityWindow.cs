#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ISecurityWindow.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface that describes a window for trading instrument creating or editing.
	/// </summary>
	public interface ISecurityWindow
	{
		/// <summary>
		/// The handler checking the entered identifier availability for <see cref="ISecurityWindow.Security"/>.
		/// </summary>
		Func<string, string> ValidateId { get; set; }

		/// <summary>
		/// Security.
		/// </summary>
		Security Security { get; set; }
	}
}