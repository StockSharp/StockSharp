#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: RiskWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using Ecng.Collections;

	using StockSharp.Algo.Risk;

	/// <summary>
	/// The window for the list editing <see cref="IRiskRule"/>.
	/// </summary>
	public partial class RiskWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RiskWindow"/>.
		/// </summary>
		public RiskWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// The list of rules added to the table.
		/// </summary>
		public IListEx<IRiskRule> Rules => Panel.Rules;
	}
}
