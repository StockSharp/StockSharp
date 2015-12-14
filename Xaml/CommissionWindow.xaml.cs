#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: CommissionWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using Ecng.Collections;

	using StockSharp.Algo.Commissions;

	/// <summary>
	/// The window for the list editing <see cref="ICommissionRule"/>.
	/// </summary>
	public partial class CommissionWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommissionWindow"/>.
		/// </summary>
		public CommissionWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// The list of rules added to the table.
		/// </summary>
		public IListEx<ICommissionRule> Rules => Panel.Rules;
	}
}