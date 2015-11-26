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
			.Register("SelectedServer", typeof(RithmicServers?), typeof(RithmicServerComboBox),
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