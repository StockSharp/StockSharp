#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rithmic.Xaml.Rithmic
File: RithmicServerComboBox.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rithmic.Xaml
{
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Xaml;

	/// <summary>
	/// The drop-down list to select <see cref="RithmicServers"/>.
	/// </summary>
	public class RithmicServerComboBox : EnumComboBox
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RithmicServerComboBox"/>.
		/// </summary>
		public RithmicServerComboBox()
		{
			EnumType = typeof(RithmicServers);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="RithmicServerComboBox.SelectedServer"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedServerProperty = DependencyProperty
			.Register(nameof(SelectedServer), typeof(RithmicServers?), typeof(RithmicServerComboBox),
			new PropertyMetadata((o, args) =>
			{
				var cb = (RithmicServerComboBox)o;
				var server = (RithmicServers?)args.NewValue;

				cb.SetSelectedValue(server);
			}));

		/// <summary>
		/// The selected server.
		/// </summary>
		public RithmicServers? SelectedServer
		{
			get { return (RithmicServers?)GetValue(SelectedServerProperty); }
			set { SetValue(SelectedServerProperty, value); }
		}
		
		/// <summary>
		/// The selected item change event handler.
		/// </summary>
		/// <param name="e">The event parameter.</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			SelectedServer = this.GetSelectedValue<RithmicServers>();
			base.OnSelectionChanged(e);
		}
	}
}