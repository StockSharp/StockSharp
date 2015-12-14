#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: SecurityTypeComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using Ecng.Xaml;

	using StockSharp.Messages;

	/// <summary>
	/// The drop-down list to select the instrument type.
	/// </summary>
	public class SecurityTypeComboBox : EnumComboBox
	{
		private const SecurityTypes _nullType = (SecurityTypes)(-1);

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityTypeComboBox"/>.
		/// </summary>
		public SecurityTypeComboBox()
		{
			EnumType = typeof(SecurityTypes);

			NullItem = new EnumComboBoxHelper.EnumerationMember
			{
				Description = string.Empty,
				Value = _nullType,
			};

			this.GetItemsSource().Insert(0, NullItem);

			SelectedType = null;
		}

		/// <summary>
		/// The entry for <see cref="SecurityTypeComboBox.SelectedType"/> which equals to <see langword="null" />.
		/// </summary>
		public EnumComboBoxHelper.EnumerationMember NullItem { get; }

		/// <summary>
		/// The selected instrument type.
		/// </summary>
		public SecurityTypes? SelectedType
		{
			get
			{
				var type = this.GetSelectedValue<SecurityTypes>();
				return type == _nullType ? null : type;
			}
			set
			{
				this.SetSelectedValue<SecurityTypes>(value ?? _nullType);
			}
		}
	}
}