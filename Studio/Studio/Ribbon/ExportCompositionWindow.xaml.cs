namespace StockSharp.Studio.Ribbon
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Logging;
	using StockSharp.Xaml.Diagram;

	public partial class ExportCompositionWindow
	{
		public static readonly RoutedCommand OkCommand = new RoutedCommand();
		public static readonly RoutedCommand CancelCommand = new RoutedCommand();

		private readonly ExportDiagramElement _composition;

		public ExportCompositionWindow(CompositionDiagramElement composition)
		{
			if (composition == null)
				throw new ArgumentNullException("composition");

			//_composition = new ExportDiagramElement(composition);

			var registry = ConfigManager.GetService<CompositionRegistry>();

			var storage = registry.Serialize(composition);
			_composition = registry.DeserializeExported(storage);

			InitializeComponent();

			PropertyGrid.SelectedObject = _composition;
		}

		private void Ok_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !Dir.Folder.IsEmpty();
		}

		private void Ok_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var cursor = Cursor;
			Cursor = Cursors.Wait;

			try
			{
				var sourcePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var destPath = Dir.Folder;
				var compositionsPath = Path.Combine(destPath, "Compositions");

				var registry = ConfigManager.GetService<CompositionRegistry>();

				var data = registry
					.Serialize(_composition)
					.SaveSettingsStorage();

				if (!PasswordCtrl.Secret.IsEmpty())
					data = data.Encrypt(PasswordCtrl.Secret.To<string>());

				File.WriteAllText(Path.Combine(destPath, "strategy.xml"), data);

				foreach (var file in Directory.GetFiles(sourcePath))
				{
					var name = Path.GetFileName(file);

					if (name == null || name.Contains("Studio"))
						continue;

					File.Copy(file, Path.Combine(destPath, name));
				}

				if (!Directory.Exists(compositionsPath))
					Directory.CreateDirectory(compositionsPath);

				foreach (var element in registry.DiagramElements.OfType<CompositionDiagramElement>())
				{
					File.WriteAllText(Path.Combine(compositionsPath, "{0}.xml".Put(element.Id)), registry.Serialize(element).SaveSettingsStorage());
				}

				DialogResult = true;
				Close();
			}
			catch (Exception ex)
			{
				ex.LogError();

				new MessageBoxBuilder()
					.Error()
					.Text("Ошибка экспорта схемы. Детальная информация указана в логе.")
					.Owner(this)
					.Button(MessageBoxButton.OK)
					.Show();
			}
			finally
			{
				Cursor = cursor;
			}
		}

		private void Cancel_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}
