#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Actipro.Code.Xaml.ActiproPublic
File: CodeReference.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml.Actipro.Code
{
	/// <summary>
	/// The link to the .NET build.
	/// </summary>
	public class CodeReference
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CodeReference"/>.
		/// </summary>
		public CodeReference()
		{
		}

		/// <summary>
		/// The build name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The path to the build.
		/// </summary>
		public string Location { get; set; }
	}
}