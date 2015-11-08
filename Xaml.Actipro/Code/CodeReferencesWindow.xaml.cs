namespace StockSharp.Xaml.Actipro.Code
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Windows;

	using Ecng.Reflection;
	using Ecng.Xaml;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Localization;

	/// <summary>
	/// The window for editing the list of references to the .NET builds.
	/// </summary>
	public partial class CodeReferencesWindow
	{
		private readonly ObservableCollection<CodeReference> _references = new ObservableCollection<CodeReference>();

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeReferencesWindow"/>.
		/// </summary>
		public CodeReferencesWindow()
		{
			InitializeComponent();

			ReferencesListView.ItemsSource = _references;
		}

		/// <summary>
		/// References.
		/// </summary>
		public IList<CodeReference> References
		{
			get { return _references; }
		}

		private void OnAddReferenceButtonClick(object sender, RoutedEventArgs e)
		{
			var dialog = new VistaOpenFileDialog
			{
				CheckFileExists = true,
				Multiselect = false,
				Filter = LocalizedStrings.Str1422
			};

			if (dialog.ShowDialog(this.GetWindow()) != true)
				return;

			var assembly = dialog.FileName.VerifyAssembly();
			if (assembly != null)
			{
				_references.Add(new CodeReference
				{
					Name = assembly.GetName().Name,
					Location = assembly.Location
				});
			}
			else
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str1423)
					.Warning()
					.Owner(this)
					.Show();
			}
		}

		private void OnRemoveReferenceButtonClick(object sender, RoutedEventArgs e)
		{
			if (ReferencesListView.SelectedItem == null)
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str1424)
					.Warning()
					.Owner(this)
					.Show();

				return;
			}

			_references.Remove((CodeReference)ReferencesListView.SelectedItem);
		}
	}
}