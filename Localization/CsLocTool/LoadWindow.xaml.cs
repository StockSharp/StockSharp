using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Ecng.Collections;
using Ecng.Common;
using Microsoft.Win32;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using MoreLinq;

namespace CsLocTool {
	/// <summary>
	/// Interaction logic for LoadWindow.xaml
	/// </summary>
	public partial class LoadWindow : Window {
		CancellationTokenSource _cts = new CancellationTokenSource();

		public LoadWindow() {
			InitializeComponent();

			Closing += OnClosing;
		}

		private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
		{
			_cts.Cancel();
		}

		readonly ObservableCollection<Project> _projects = new ObservableCollection<Project>(); 
		public IEnumerable<Project> Projects { get { return _projects; } }

		public Solution Solution {get; private set;}
		public string CsvFilePath {get; private set;}

		private async void SelectSolution_Click(object sender, RoutedEventArgs args)
		{
			var newPath = SelectSolutionFile(_tbSolutionPath.Text.Trim());
			if(newPath.IsEmpty())
				return;

			_projects.Clear();
			_tbSolutionPath.Text = newPath;

			var workspace = MSBuildWorkspace.Create();

			_busyIndicator.IsBusy = true;

			try
			{
				_cts = new CancellationTokenSource();
				Solution = null;
				Solution = await workspace.OpenSolutionAsync(newPath, _cts.Token);
			}
			catch (Exception e)
			{
				ShowError("Unable to open solution:\n" + e);
			}
			finally
			{
				_busyIndicator.IsBusy = false;
			}

			if(Solution == null)
				return;

			Solution.Projects.ForEach(p => _projects.Add(p));
		}

		private void RemoveProject_Click(object sender, RoutedEventArgs e)
		{
			_projects.RemoveRange(_dataGridProjects.SelectedItems.Cast<Project>().ToArray());
		}

		private void ButtonLoad_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = null;

			if (!Projects.Any())
			{
				ShowError("At least one project must be in the project list.");
				return;
			}

			if (CsvFilePath.IsEmpty() || !CsvFilePath.EndsWith(".csv", true, CultureInfo.InvariantCulture))
			{
				ShowError("You must set CSV file location.");
				return;
			}

//			if (CsFilePath.IsEmpty() || !CsFilePath.EndsWith(".cs", true, CultureInfo.InvariantCulture))
//			{
//				ShowError("You must set CS file location.");
//				return;
//			}

			DialogResult = true;
			//Close();
		}

		private void SelectCsv_Click(object sender, RoutedEventArgs e)
		{
			var newPath = SelectCsvFile(_tbCsvPath.Text.Trim());
			if(newPath.IsEmpty())
				return;

			CsvFilePath = newPath;
			_tbCsvPath.Text = CsvFilePath;
		}

		void ShowError(string message)
		{
			MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		static string SelectSolutionFile(string currentSelection) {
			string dir;

			try
			{
				dir = !string.IsNullOrEmpty(currentSelection) ?
						  Path.GetDirectoryName(currentSelection) : (MainWindow.LastSelectDirectory ?? Directory.GetCurrentDirectory());
			}
			catch
			{
				dir = string.Empty;
			}

			var dialog = new OpenFileDialog
			{
				Filter = "Solution files (*.sln)|*.sln", 
				Title = "Load solution",
				CheckFileExists = true, 
				Multiselect = false, 
				InitialDirectory = dir
			};

			if (dialog.ShowDialog() == true)
			{
				MainWindow.LastSelectDirectory = Path.GetDirectoryName(dialog.FileName);
				return dialog.FileName;
			}

			return null;
		}

		static string SelectCsvFile(string currentSelection) {
			string dir;

			try
			{
				dir = !string.IsNullOrEmpty(currentSelection) ?
						  Path.GetDirectoryName(currentSelection) : (MainWindow.LastSelectDirectory ?? Directory.GetCurrentDirectory());
			}
			catch
			{
				dir = string.Empty;
			}

			var dialog = new OpenFileDialog
			{
				Filter = "CSV files (*.csv)|*.csv", 
				Title = "Create or load CSV file",
				CheckFileExists = false, 
				Multiselect = false, 
				InitialDirectory = dir
			};

			if (dialog.ShowDialog() == true)
			{
				MainWindow.LastSelectDirectory = Path.GetDirectoryName(dialog.FileName);
				return dialog.FileName;
			}

			return null;
		}
	}
}
