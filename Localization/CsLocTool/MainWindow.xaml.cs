using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using ActiproSoftware.Text;
using ActiproSoftware.Text.Languages.CSharp.Implementation;
using Ecng.Collections;
using Ecng.Common;
using Ecng.Localization;
using Ecng.Xaml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Win32;
using MoreLinq;

namespace CsLocTool 
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window 
	{
		public readonly static CultureInfo RuCulture = new CultureInfo("ru-RU");
		LoadWindow _loadWindow;

		Solution _solution;
		List<Project> _projects;

		readonly List<SourceCodeLiteral> _allLiterals = new List<SourceCodeLiteral>(); 
		readonly List<SourceCodeLiteral> _chosenLiterals = new List<SourceCodeLiteral>(); 
		readonly List<StringResource> _allResources = new List<StringResource>(); 

		readonly CSharpSyntaxLanguage _actiproCSharpLanguage = new CSharpSyntaxLanguage();

		readonly Regex _regExRussianText = new Regex(@"\p{IsCyrillic}", RegexOptions.Compiled);

		public static string LastSelectDirectory {get; set;}

		FileFilterWindow _filterWindow;

		#region dependency properties

		public static readonly DependencyProperty FilteredLiteralsProperty = DependencyProperty.Register("FilteredLiterals", typeof(IEnumerable<SourceCodeLiteral>), typeof(MainWindow), new PropertyMetadata(default(IEnumerable<SourceCodeLiteral>)));
		public static readonly DependencyProperty FilteredChosenLiteralsProperty = DependencyProperty.Register("FilteredChosenLiterals", typeof(IEnumerable<SourceCodeLiteral>), typeof(MainWindow), new PropertyMetadata(default(IEnumerable<SourceCodeLiteral>)));
		public static readonly DependencyProperty FilteredResourcesProperty = DependencyProperty.Register("FilteredResources", typeof(IEnumerable<StringResource>), typeof(MainWindow), new PropertyMetadata(default(IEnumerable<StringResource>)));
		public static readonly DependencyProperty LiteralInEditorProperty = DependencyProperty.Register("LiteralInEditor", typeof(SourceCodeLiteral), typeof(MainWindow), new PropertyMetadata(default(SourceCodeLiteral)));
		public static readonly DependencyProperty CsvFilePathProperty = DependencyProperty.Register("CsvFilePath", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));
		public static readonly DependencyProperty CsFilePathProperty = DependencyProperty.Register("CsFilePath", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));
		public static readonly DependencyProperty CsFileNamespaceProperty = DependencyProperty.Register("CsFileNamespace", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));
		public static readonly DependencyProperty NumAllFilteredProperty = DependencyProperty.Register("NumAllFiltered", typeof (string), typeof (MainWindow), new PropertyMetadata(default(string)));
		public static readonly DependencyProperty NumChosenFilteredProperty = DependencyProperty.Register("NumChosenFiltered", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

		public IEnumerable<SourceCodeLiteral> FilteredLiterals
		{
			get { return (IEnumerable<SourceCodeLiteral>) GetValue(FilteredLiteralsProperty); }
			set { SetValue(FilteredLiteralsProperty, value); }
		}

		public IEnumerable<SourceCodeLiteral> FilteredChosenLiterals
		{
			get { return (IEnumerable<SourceCodeLiteral>) GetValue(FilteredChosenLiteralsProperty); }
			set { SetValue(FilteredChosenLiteralsProperty, value); }
		}

		public IEnumerable<StringResource> FilteredResources
		{
			get { return (IEnumerable<StringResource>) GetValue(FilteredResourcesProperty); }
			set { SetValue(FilteredResourcesProperty, value); }
		}

		public SourceCodeLiteral LiteralInEditor
		{
			get { return (SourceCodeLiteral) GetValue(LiteralInEditorProperty); }
			set { SetValue(LiteralInEditorProperty, value); }
		}

		public string CsvFilePath
		{
			get { return (string) GetValue(CsvFilePathProperty); }
			set { SetValue(CsvFilePathProperty, value); }
		}

		public string CsFilePath
		{
			get { return (string) GetValue(CsFilePathProperty); }
			set { SetValue(CsFilePathProperty, value); }
		}

		public string CsFileNamespace
		{
			get { return (string) GetValue(CsFileNamespaceProperty); }
			set { SetValue(CsFileNamespaceProperty, value); }
		}

		public string NumAllFiltered
		{
			get { return (string) GetValue(NumAllFilteredProperty); }
			set { SetValue(NumAllFilteredProperty, value); }
		}

		public string NumChosenFiltered
		{
			get { return (string) GetValue(NumChosenFilteredProperty); }
			set { SetValue(NumChosenFilteredProperty, value); }
		}

		#endregion

		static readonly HashSet<string> SupportedAttributes = new HashSet<string>
		{
			"Category",
			"CategoryOrder",
			"Description",
			"DisplayName",
			"EnumDisplayName",
		};

		public MainWindow() {
			InitializeComponent();

			_filterWindow = new FileFilterWindow();
			_filterWindow.MakeHideable();

			Loaded += (sender, args) =>
			{
				_radioSortLiteralsAlpha.IsChecked = true;
				_checkOnlyRussianLiterals.IsChecked = true;
				_syntaxEditor.Document.IsReadOnly = true;
				_checkSelectNonAttributesLiterals.IsChecked = true;
				_checkSelectSupportedFiles.IsChecked = true;
				_checkSelectNonCase.IsChecked = true;
				_checkSelectSupportedAttributesLiterals.IsChecked = true;
				_checkSelectNonConst.IsChecked = true;

				_filterWindow.Owner = this;
			};

			Closing += (sender, args) =>
			{
				SaveChosenLiterals();
				_filterWindow.DeleteHideable();
				_filterWindow.Close();
			};
		}

		#region UI handlers

		private async void LoadWorkspace_Click(object sender, RoutedEventArgs e)
		{
			_loadWindow = new LoadWindow { Owner = this };

			SaveChosenLiterals();

			var result = _loadWindow.ShowDialog();

			if (_loadWindow.DialogResult == true)
			{
				_solution = _loadWindow.Solution;
				_projects = _loadWindow.Projects.ToList();
				CsvFilePath = _loadWindow.CsvFilePath;

				await ReloadWorkspace();
			}
		}

		private void ApplyLiteralsFilterSelectArgs(object sender, RoutedEventArgs args)
		{
			ApplyLiteralsFilterSelect();
		}

		private void ApplyResourcesFilterSelectArgs(object sender, RoutedEventArgs args)
		{
			ApplyResourcesFilterSelect();
		}

		private void _listLiterals_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
			{
				var selected = _listLiterals.SelectedItems.Cast<SourceCodeLiteral>().ToArray();
				foreach (var l in selected)
					_allLiterals.Remove(l);

				ApplyLiteralsFilterSelect();
			}
		}

		private void _listLiterals_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			ShowSelectedLiteralInEditor();
		}

		private void _listLiteralsForCurrentResource_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
			{
				//MessageBox.Show("del2");
			}
		}

		private void _listChosenLiterals_OnKeyDown(object sender, KeyEventArgs e)
		{
		}

		private void _listChosenLiterals_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			ShowSelectedChosenLiteralInEditor();
		}

		private void _listChosenLiterals_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ShowSelectedChosenLiteralInEditor();
		}

		private void _listLiteralsForCurrentResource_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ShowSelectedLiteralFromResourceListInEditor();
		}

		private void _listLiteralsForCurrentResource_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			ShowSelectedLiteralFromResourceListInEditor();
		}

		private void _listLiterals_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ShowSelectedLiteralInEditor();
		}

		private void AddNewResource_Click(object sender, RoutedEventArgs e)
		{
			var literal = _listChosenLiterals.SelectedItem as SourceCodeLiteral;

			var newRes = new StringResource(true) { ConstantName = GenerateNewResourceName(literal), IsModified = false };
			_allResources.Add(newRes);

			var selected = ApplyResourcesFilterSelect(newRes);

			if (selected)
			{
				_dataGridResources.CurrentCell = new DataGridCellInfo(newRes, _dataGridResources.Columns[0]);
				_dataGridResources.Focus();
				_dataGridResources.BeginEdit();
			}
		}

		private void CopyStringToResource_Click(object sender, RoutedEventArgs e)
		{
			var selectedResource = _dataGridResources.SelectedItem as StringResource;
			if (selectedResource == null)
			{
				ShowError("You must select resource first.");
				return;
			}

			if (_listLiteralsForCurrentResource.SelectedItems.Count != 1)
			{
				ShowError("You must select single string from the string list for current resource.");
				return;
			}

			var selectedLiteral = _listLiteralsForCurrentResource.SelectedItems.Cast<SourceCodeLiteral>().Single();

			selectedResource.EngString = selectedResource.RusString = selectedLiteral.StringValue;
		}

		private void MoveStringToResource_Click(object sender, RoutedEventArgs e)
		{
			var selectedResource = _dataGridResources.SelectedItem as StringResource;
			if (selectedResource == null)
			{
				ShowError("You must select resource first.");
				return;
			}

			var selectedStrings = _listChosenLiterals.SelectedItems.Cast<SourceCodeLiteral>().ToList();
			if(selectedStrings.Count == 0)
				return;

			selectedResource.AddLiterals(selectedStrings);
			selectedStrings.ForEach(s => _chosenLiterals.Remove(s));

			ApplyLiteralsFilter();

			var item = selectedStrings.First();
			if(_listLiteralsForCurrentResource.Items.Contains(item))
				_listLiteralsForCurrentResource.SelectedItem = item;
		}

		private void MoveResourceToString_Click(object sender, RoutedEventArgs e)
		{
			var selectedResource = _dataGridResources.SelectedItem as StringResource;
			if (selectedResource == null)
			{
				ShowError("You must select resource first.");
				return;
			}

			var selectedStrings = _listLiteralsForCurrentResource.SelectedItems.Cast<SourceCodeLiteral>().ToList();
			if(selectedStrings.Count == 0)
				return;

			selectedResource.RemoveLiterals(selectedStrings);
			_chosenLiterals.AddRange(selectedStrings);

			ApplyLiteralsFilter();
		}

		private void SaveCsv_Click(object sender, RoutedEventArgs e)
		{
			SaveCSV();
		}

		private void SaveCs_Click(object sender, RoutedEventArgs e)
		{
			SaveCS();
		}

		private async void UpdateSource_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(this,
				"Make sure you have your code checked in before you proceed so your could revert automatic code changes if something goes wrong. CSV and CS(properties) files will be saved as well.\nDo you want to proceed with source code update?",
				"Proceed with code update?", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
			{
				return;
			}

			if(!SaveCSV() || !SaveCS())
				return;

			var dict = new Dictionary<Document, List<Tuple<SourceCodeLiteral, StringResource>>>();

			_busyIndicator.BusyContent = "Searching for changes...";
			_busyIndicator.IsBusy = true;

			var updatedFiles = 0;
			var updatedLiterals = 0;
			var updatedNames = 0;
			var updatedUsings = 0;

			var projects = new HashSet<string>();

			try
			{
				foreach (var res in _allResources)
				{
					foreach (var literal in res.Literals)
					{
						var list = dict.TryGetValue(literal.Document);
						if (list == null)
							dict.Add(literal.Document, list = new List<Tuple<SourceCodeLiteral, StringResource>>());

						list.Add(Tuple.Create(literal, res));
					}
				}

				var nspace = _tbNamespace.Text.Trim();
				var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nspace)).NormalizeWhitespace();

				foreach (var pair in dict)
				{
					var doc = pair.Key;
					var list = pair.Value;

					projects.Add(doc.Project.FilePath);

					_busyIndicator.BusyContent = "Applying changes to {0}...".Put(doc.FilePath);

					var root = (CompilationUnitSyntax)doc.GetSyntaxRootAsync().Result;

					var replaceDict = new Dictionary<SyntaxNode, SyntaxNode>();

					foreach (var t in list)
					{
						var literal = t.Item1;
						var resource = t.Item2;

						if(literal.IsConstString || literal.IsSwitchCase)
							continue;

						if(literal.IsPartOfAttributeDeclaration && !SupportedAttributes.Contains(literal.AttributeName))
							continue;

						if (literal.IsPartOfAttributeDeclaration)
						{
							NameSyntax name = SyntaxFactory.IdentifierName("LocalizedStrings");
							name = SyntaxFactory.QualifiedName(name, SyntaxFactory.IdentifierName(resource.ConstantName + NameSuffix));
							name = name.WithLeadingTrivia(literal.Expression.GetLeadingTrivia())
										.WithTrailingTrivia(literal.Expression.GetTrailingTrivia());

							replaceDict.Add(literal.Expression, name);

							name = SyntaxFactory.IdentifierName(literal.AttributeName + "Loc");
							name = name.WithLeadingTrivia(literal.AttributeNameSyntax.GetLeadingTrivia())
										.WithTrailingTrivia(literal.AttributeNameSyntax.GetTrailingTrivia());

							replaceDict.Add(literal.AttributeNameSyntax, name);
						}
						else
						{
							NameSyntax name = SyntaxFactory.IdentifierName("LocalizedStrings");
							name = SyntaxFactory.QualifiedName(name, SyntaxFactory.IdentifierName(resource.ConstantName));
							name = name.WithLeadingTrivia(literal.Expression.GetLeadingTrivia())
										.WithTrailingTrivia(literal.Expression.GetTrailingTrivia());

							replaceDict.Add(literal.Expression, name);
						}
					}

					var rewriter = new MyRewriter(replaceDict);
					root = (CompilationUnitSyntax)rewriter.Visit(root);

//					var nspaceSyntax = SourceCodeLiteral.FindChildNode<NamespaceDeclarationSyntax>(root, 1);
//					if (nspaceSyntax != null && rewriter.NumReplacedLiterals + rewriter.NumReplacedNames > 0)
//					{
//						root = root.ReplaceNode(nspaceSyntax, AddUsing(nspaceSyntax, usingDirective));
//						++updatedUsings;
//					}

					File.WriteAllText(doc.FilePath, root.ToFullString());
					++updatedFiles;
					updatedLiterals += rewriter.NumReplacedLiterals;
					updatedNames += rewriter.NumReplacedNames;
				}
			}
			finally
			{
				_busyIndicator.IsBusy = false;
			}

			MessageBox.Show(this, "Successfully updated {0} C# files (updated {1} literals, {2} names, {3} usings)".Put(updatedFiles, updatedLiterals, updatedNames, updatedUsings), "Done", MessageBoxButton.OK, MessageBoxImage.Information);

			File.WriteAllLines("updated_projects.txt", projects.OrderBy(p => p.Count(c => c == '\\')).ThenBy(p => p));

			await ReloadWorkspaceFull();
		}

		private static NamespaceDeclarationSyntax AddUsing(NamespaceDeclarationSyntax nspaceSyntax, UsingDirectiveSyntax newUsing)
		{
			if (nspaceSyntax.Usings.Count > 0)
			{
				var lastUsing = nspaceSyntax.Usings.Last();
				newUsing = newUsing
					.WithLeadingTrivia(lastUsing.GetLeadingTrivia())
					.WithTrailingTrivia(lastUsing.GetTrailingTrivia());

				return nspaceSyntax.WithUsings(nspaceSyntax.Usings.Add(newUsing));
			}

			return nspaceSyntax.WithUsings(nspaceSyntax.Usings.Add(newUsing.NormalizeWhitespace()));
		}

		private void SelectCs_Click(object sender, RoutedEventArgs e)
		{
			var newPath = SelectCsFile(CsFilePath != null ? CsFilePath.Trim() : null);
			if(newPath.IsEmpty())
				return;

			CsFilePath = newPath;
		}

		private void Choose_Click(object sender, RoutedEventArgs e)
		{
			var selectedLiterals = _listLiterals.SelectedItems.Cast<SourceCodeLiteral>().ToList();
			if(selectedLiterals.Count == 0)
				return;

			selectedLiterals.ForEach(s =>
			{
				_allLiterals.Remove(s);
				_chosenLiterals.Add(s);
			});

			ApplyLiteralsFilter();
		}

		private void Unchoose_Click(object sender, RoutedEventArgs e)
		{
			var selectedLiterals = _listChosenLiterals.SelectedItems.Cast<SourceCodeLiteral>().ToList();
			if(selectedLiterals.Count == 0)
				return;

			selectedLiterals.ForEach(s =>
			{
				_chosenLiterals.Remove(s);
				_allLiterals.Add(s);
			});

			ApplyLiteralsFilter();
		}

		private void SaveAllStrings_Click(object sender, RoutedEventArgs e)
		{
//			var names = FilteredLiterals
//				.Where(l => l.IsPartOfAttributeDeclaration)
//				.GroupBy(l => l.AttributeName)
//				.Select(g => g.Key)
//				.OrderBy(n => n)
//				.ToList();
//			File.WriteAllText("attributes.txt", names.Join("\n"));
//			return;

			var groups = FilteredLiterals
				.GroupBy(l => l.OriginalText)
				.OrderBy(g => g.Key)
				.ToList();

			var resId = 0;

			using(var writer = new CsvFileWriter("resources.csv"))
				foreach (var g in groups)
				{
					var str = g.Key;
					if(str[str.Length-1] == '"' && str[0] == '"')
						str = str.Substring(1, str.Length-2);

					++resId;

					writer.WriteRow(new List<string>
					{
						resId.ToString(CultureInfo.InvariantCulture),
						g.Count().ToString(CultureInfo.InvariantCulture),
						"Str{0}".Put(resId),
						str,
						str
					});
				}
		}

		private void GenerateResources_Click(object sender, RoutedEventArgs e)
		{
			_allResources.Clear();

			foreach (var group in _chosenLiterals.GroupBy(l => l.StringValue))
			{
				var first = group.First();

				var newRes = new StringResource(true, group)
				{
					ConstantName = GenerateNewResourceName(first),
					EngString = first.StringValue,
					RusString = first.StringValue,
					IsModified = true
				};
				_allResources.Add(newRes);
			}


			ApplyResourcesFilterSelect();
		}

		private void FilesFilterButton_Click(object sender, RoutedEventArgs e)
		{
			_filterWindow.ShowDialog();

			ApplyLiteralsFilterSelect();
		}

		private void SaveXamlResources_Click(object sender, RoutedEventArgs e)
		{
			var xamlResources = Enumerable.Empty<XamlResource>();

			try
			{
				foreach (var file in GetXamlList())
				{
					var doc = new XmlDocument();
					doc.Load(file);
					var root = doc.DocumentElement;
					if (root == null) continue;

					xamlResources = xamlResources.Concat(GetRussianStrings(root));
				}

				xamlResources = xamlResources.ToArray();

				var strings = xamlResources.GroupBy(r => r.TextTrim).Select(g => g.Key).OrderBy(s => s);
				var index = 0;

				using(var writer = new CsvFileWriter("xaml_resources.csv"))
					foreach (var str in strings)
					{
						var name = "XamlStr{0}".Put(++index);
						writer.WriteRow(new List<string> {name, name, str, str});
					}

				var tuples = xamlResources.GroupBy(r => Tuple.Create(r.ElementName, r.AttrName)).Select(g => g.Key).OrderBy(t => t.Item1).ThenBy(t => t.Item2);
				using(var writer = new CsvFileWriter("xaml_tags.csv"))
					foreach (var t in tuples)
					{
						writer.WriteRow(new List<string> {t.Item1, t.Item2});
					}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "error", ex.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void UpdateXaml_Click(object sender, RoutedEventArgs e)
		{
			if (_allResources.IsEmpty())
			{
				MessageBox.Show(this, "Error", "No resources loaded", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (MessageBox.Show(this,
				"Make sure you have your code checked in before you proceed so your could revert automatic code changes if something goes wrong.\nDo you want to proceed with XAML files update?",
				"Proceed with XAML update?", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
			{
				return;
			}

			var xamlResources = Enumerable.Empty<XamlResource>();
			var resNotFoundForList = new List<XamlResource>();
			var unableToReplaceList = new List<XamlResource>();
			var trimUsedList = new List<XamlResource>();
			var moreThanOneList = new List<XamlResource>();
			var failedNamespaceList = new List<string>();

			var numFiles = 0;
			var numReplacements = 0;

			try
			{
				foreach (var f in GetXamlList())
				{
					var file = f;
					var doc = new XmlDocument();
					doc.Load(file);
					var root = doc.DocumentElement;
					if (root == null) continue;

					var arr = GetRussianStrings(root).ToArray();
					arr.ForEach(r => r.Filename = file);
					xamlResources = xamlResources.Concat(arr);
				}

				#region replace

				foreach (var group in xamlResources.GroupBy(r => r.Filename))
				{
					var filename = group.Key;
					var text = File.ReadAllText(filename);
					var oldText = text;

					var replaced = new HashSet<string>();

					foreach (var res in group)
					{
						bool trim;
						var replacement = res.GetReplacementText(_allResources, out trim);

						if (replacement == null)
						{
							if (trim)
								trimUsedList.Add(res);
							resNotFoundForList.Add(res);
							continue;
						}

						string origText;
						if (trim)
						{
							trimUsedList.Add(res);
							origText = res.TextTrim;
						}
						else
						{
							origText = res.Text;
						}

						if(filename == @"Z:\src\git\stocksharp\Xaml\CommissionPanel.xaml") {}

						if (!res.IsDefaultText && !trim)
						{
							origText = '"' + origText + '"';
							replacement = '"' + replacement + '"';
						}
						else if (trim)
						{
						}

						var count = new Regex(Regex.Escape(origText)).Matches(text).Count;

						if (count == 0)
						{
							if(!replaced.Contains(res.TextTrim))
								unableToReplaceList.Add(res);

							continue;
						}

						if(count > 1)
							moreThanOneList.Add(res);

						var newText = text.Replace(origText, replacement);
						if (text != newText)
						{
							replaced.Add(res.TextTrim);
							text = newText;
							numReplacements++;
						}
						else
						{
							unableToReplaceList.Add(res);
						}
					}

					if(text == oldText)
						continue;

					numFiles++;

					var tmp = AddXamlNamespace(text);
					if(tmp == null)
						failedNamespaceList.Add(filename);
					else
						text = tmp;

					File.WriteAllText(filename, text);
				}
				#endregion

				using (var file = new StreamWriter("xaml_report.txt"))
				{
					file.WriteLine("resources not found:");
					file.WriteLine(resNotFoundForList.Select(r => "{0}: {1}".Put(r.Filename, r.Text)).Join("\n") + "\n\n");

					file.WriteLine("unable to replace:");
					file.WriteLine(unableToReplaceList.Select(r => "{0}: {1}".Put(r.Filename, r.Text)).Join("\n") + "\n\n");

					file.WriteLine("trim used:");
					file.WriteLine(trimUsedList.Select(r => "{0}: {1}".Put(r.Filename, r.Text)).Join("\n") + "\n\n");

					file.WriteLine("more than one replacement:");
					file.WriteLine(moreThanOneList.Select(r => "{0}: {1}".Put(r.Filename, r.Text)).Join("\n") + "\n\n");

					file.WriteLine("failed to fix namespace:");
					file.WriteLine(failedNamespaceList.Join("\n"));
				}

				MessageBox.Show(this, "Done!\nFile: {0}\nReplacements: {1}".Put(numFiles, numReplacements));
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "error", ex.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region workspace loading

		private async Task ReloadWorkspaceFull()
		{
			var workspace = MSBuildWorkspace.Create();
			_busyIndicator.IsBusy = true;
			_busyIndicator.BusyContent = "Reloading solution...";

			var projNames = _projects.Select(p => p.Name).ToList();

			try
			{
				var path = _solution.FilePath;
				_solution = null;
				_projects.Clear();

				_solution = await workspace.OpenSolutionAsync(path);
			}
			catch (Exception e)
			{
				ShowError("Unable to open solution:\n" + e);
			}
			finally
			{
				_busyIndicator.IsBusy = false;
			}

			if(_solution == null)
				return;

			_solution.Projects.ForEach(p =>
			{
				if(projNames.Contains(p.Name))
					_projects.Add(p);
			});
			
			await ReloadWorkspace();
		}

		private async Task ReloadWorkspace()
		{
			_busyIndicator.IsBusy = true;

			try
			{
				_allLiterals.Clear();
				_allResources.Clear();

				await ParseAllStringLiterals();

				RestoreSavedChosen();
				_filterWindow.ReloadFilters(_solution.FilePath);

				ApplyLiteralsFilter();

				_busyIndicator.BusyContent = "Loading CSV...";

				LoadCSV();
				ApplyResourcesFilter();
			}
			finally
			{
				_busyIndicator.IsBusy = false;
			}
		}

		private async Task ParseAllStringLiterals()
		{
			var tcs = new CancellationTokenSource();
			var numDocuments = _projects.Sum(p => p.Documents.Count(d => d.SupportsSyntaxTree));
			var loadedCount = 0;

			foreach (var p in _projects)
			{
				foreach (var d in p.Documents.Where(d => d.SupportsSyntaxTree))
				{
					var tree = await d.GetSyntaxTreeAsync(tcs.Token);
					var root = await tree.GetRootAsync(tcs.Token);

					++loadedCount;
					_busyIndicator.BusyContent = "Processed files: {0}/{1}...".Put(loadedCount, numDocuments);

					_allLiterals.AddRange(root.DescendantNodes().OfType<LiteralExpressionSyntax>()
						.Where(s => s.CSharpKind() == SyntaxKind.StringLiteralExpression)
						.Select(e => new SourceCodeLiteral(d, e)));
				}
			}

			_busyIndicator.BusyContent = "Please wait...";

			var alldocs = _projects.SelectMany(p => p.Documents);
			var allDocuments = new Dictionary<string, Document>();
			foreach(var d in alldocs)
				allDocuments[d.FilePath] = d;

			foreach (var g in _allLiterals.GroupBy(l => l.FilePath))
			{
				var path = g.Key;
				var doc = allDocuments[path];

				var lines = doc.GetTextAsync().Result.Lines;

				foreach(var l in g)
					l.InitCodeLine(lines);
			}
		}

		private void LoadCSV()
		{
			if(!File.Exists(CsvFilePath))
				return;

			var strings = new List<StringResource>();
			var columns = new List<string>();
			using (var reader = new CsvFileReader(CsvFilePath, EmptyLineBehavior.Ignore))
				while (reader.ReadRow(columns))
				{
					if (columns.Count == 0)
						continue;

					if (columns.Count != 3)
					{
						ShowError("Error parsing CSV file. Wrong number of columns({0}): {1}".Put(columns.Count, columns.Join("|")));
						return;
					}

					strings.Add(new StringResource(false)
					{
						ConstantName = columns[0],
						EngString = columns[1],
						RusString = columns[2],
						IsModified = false
					});
				}

			_allResources.Clear();
			_allResources.AddRange(strings);
		}

		#endregion

		#region filters

		void ApplyLiteralsFilter()
		{
			var allFiltered = FilterLiterals(_allLiterals);
			var chosenFiltered = FilterLiterals(_chosenLiterals);

			ObservableCollection<SourceCodeLiteral> coll1, coll2;

			if (_radioSortLiteralsAlpha.IsChecked == true)
			{
				coll1 = new ObservableCollection<SourceCodeLiteral>(allFiltered.OrderBy(l => l.OriginalText));
				coll2 = new ObservableCollection<SourceCodeLiteral>(chosenFiltered.OrderBy(l => l.OriginalText));
			}
			else
			{
				coll1 = new ObservableCollection<SourceCodeLiteral>(allFiltered.OrderBy(l => l.CodeOrder));
				coll2 = new ObservableCollection<SourceCodeLiteral>(chosenFiltered.OrderBy(l => l.CodeOrder));
			}

			FilteredLiterals = coll1;
			FilteredChosenLiterals = coll2;

			NumAllFiltered = "{0}/{1}".Put(coll1.Count, _allLiterals.Count);
			NumChosenFiltered = "{0}/{1}".Put(coll2.Count, _chosenLiterals.Count);
		}

		IEnumerable<SourceCodeLiteral> FilterLiterals(IEnumerable<SourceCodeLiteral> literals)
		{
			var onlyRussian = _checkOnlyRussianLiterals.IsChecked == true;
			var fileFilter = _tbFileSearch.Text.Trim().ToLower(MainWindow.RuCulture);
			var txtFilter = _tbLiteralsSearch.Text.Trim().ToLower(MainWindow.RuCulture);
			var txtLineFilter = _tbLiteralsSearchLine.Text.Trim().ToLower(MainWindow.RuCulture);

			if(txtLineFilter.IsEmpty()) txtLineFilter = null;
			if(txtFilter.IsEmpty()) txtFilter = null;
			if(fileFilter.IsEmpty()) fileFilter = null;

			var selectSupportedAttrs = _checkSelectSupportedAttributesLiterals.IsChecked == true;
			var selectUnsupportedAttrs = _checkSelectUnsupportedAttributesLiterals.IsChecked == true;
			var selectNonAttributes = _checkSelectNonAttributesLiterals.IsChecked == true;
			var selectSupportedFiles = _checkSelectSupportedFiles.IsChecked == true;
			var selectUnsupportedFiles = _checkSelectUnsupportedFiles.IsChecked == true;
			var selectCase = _checkSelectCase.IsChecked == true;
			var selectNonCase = _checkSelectNonCase.IsChecked == true;
			var selectConst = _checkSelectConst.IsChecked == true;
			var selectNonConst = _checkSelectNonConst.IsChecked == true;
			var invertFileSearch = _checkFileSearchInvert.IsChecked == true;
			var invertLineSearch = _checkLineSearchInvert.IsChecked == true;
			var invertLiteralSearch = _checkLiteralsSearchInvert.IsChecked == true;

			var fileFilters = _filterWindow.GetFilesFilter();

			return literals.Where(l => 
				(!onlyRussian || ContainsRussianText(l.Expression)) && 
				(selectCase && l.IsSwitchCase || selectNonCase && !l.IsSwitchCase) &&
				(selectConst && l.IsConstString || selectNonConst && !l.IsConstString) &&
				(selectUnsupportedFiles && fileFilters.Any(r => r.IsMatch(l.FilePath)) || selectSupportedFiles && fileFilters.All(r => !r.IsMatch(l.FilePath))) &&
				(l.ContainsText(txtFilter) ^ invertLiteralSearch) &&
				(l.MatchFile(fileFilter) ^ invertFileSearch) &&
				(l.CodeLineContainsText(txtLineFilter) ^ invertLineSearch) &&
				(l.IsPartOfAttributeDeclaration && (selectSupportedAttrs && SupportedAttributes.Contains(l.AttributeName) ||
													selectUnsupportedAttrs && !SupportedAttributes.Contains(l.AttributeName)) || 
				 selectNonAttributes && !l.IsPartOfAttributeDeclaration));
		}

		void ApplyLiteralsFilterSelect()
		{
			var selectedItem = _listLiterals.SelectedItem as SourceCodeLiteral;

			ApplyLiteralsFilter();

			if (selectedItem != null && FilteredLiterals.Contains(selectedItem))
			{
				_listLiterals.SelectedItem = selectedItem;
				_listLiterals.ScrollIntoView(selectedItem);
			}
		}

		private bool ApplyResourcesFilterSelect(StringResource trySelect = null)
		{
			var selectedItem = _dataGridResources.SelectedItem as StringResource;

			ApplyResourcesFilter();

			if (trySelect != null)
			{
				if (FilteredResources.Contains(trySelect))
				{
					_dataGridResources.SelectedItem = trySelect;
					_dataGridResources.ScrollIntoView(trySelect);

					return true;
				}

				return false;
			} 

			if (selectedItem != null && FilteredResources.Contains(selectedItem))
			{
				_dataGridResources.SelectedItem = selectedItem;
				_dataGridResources.ScrollIntoView(selectedItem);

				return true;
			}

			return false;
		}

		void ApplyResourcesFilter()
		{
			var newOrModifiedOnly = _checkNewModifiedOnlyResources.IsChecked == true;
			var filter = _tbResourcesSearch.Text.Trim().ToLower(MainWindow.RuCulture);

			FilteredResources = new ObservableCollection<StringResource>(
				_allResources.Where(s => s.ContainsText(filter) && (!newOrModifiedOnly || s.IsNewOrModified)));
		}

		#endregion

		#region editor

		void ShowLiteralInEditor(SourceCodeLiteral literal)
		{
			if(LiteralInEditor == literal)
				return;

			var text = literal.Document.GetTextAsync().Result.ToString();
			using(var stream = GenerateStreamFromString(text))
				_syntaxEditor.Document.LoadFile(stream, Encoding.UTF8);

			_syntaxEditor.ActiveView.ScrollToCaretOnSelectionChange = true;

			_syntaxEditor.Document.Language = _actiproCSharpLanguage;
			_syntaxEditor.Document.IsReadOnly = true;

			var lineSpan = literal.Expression.SyntaxTree.GetLineSpan(literal.Expression.Span);
			var start = lineSpan.StartLinePosition;
			var end = lineSpan.EndLinePosition;

			_syntaxEditor.ActiveView.Selection.StartPosition = new TextPosition(start.Line, start.Character);
			_syntaxEditor.ActiveView.Selection.EndPosition = new TextPosition(end.Line, end.Character);

			LiteralInEditor = literal;
		}

		void ShowSelectedLiteralInEditor()
		{
			var selected = _listLiterals.SelectedItems.Cast<SourceCodeLiteral>().FirstOrDefault();
			if(selected == null) return;

			ShowLiteralInEditor(selected);
		}

		void ShowSelectedChosenLiteralInEditor()
		{
			var selected = _listChosenLiterals.SelectedItems.Cast<SourceCodeLiteral>().FirstOrDefault();
			if(selected == null) return;

			ShowLiteralInEditor(selected);
		}

		void ShowSelectedLiteralFromResourceListInEditor()
		{
			var selected = _listLiteralsForCurrentResource.SelectedItems.Cast<SourceCodeLiteral>().FirstOrDefault();
			if(selected == null) return;

			ShowLiteralInEditor(selected);
		}

		#endregion

		#region helpers

		bool ContainsRussianText(LiteralExpressionSyntax expression)
		{
			return _regExRussianText.IsMatch(expression.Token.Text);
		}

		public static Stream GenerateStreamFromString(string s)
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}

		public static string GetStringHash(string str) {
			using(var stream = GenerateStreamFromString(str))
			using(var sha1 = new SHA1Managed()) {
				var hash = sha1.ComputeHash(stream);
				var formatted = new StringBuilder(2*hash.Length);
				foreach(var b in hash)
					formatted.AppendFormat("{0:x2}", b);

				return formatted.ToString();
			}
		}

		string GenerateNewResourceName(SourceCodeLiteral literal)
		{
			var n = _allResources.Count + 1;
			while(_allResources.FirstOrDefault(r => r.ConstantName.StartsWith("Str{0}".Put(n))) != null)
				++n;

			return literal != null && literal.StringValue.Contains("{") && literal.StringValue.Contains("}") ?
				"Str{0}Params".Put(n) :
				"Str{0}".Put(n);
		}

		void ShowError(string message)
		{
			MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		static string SelectCsFile(string currentSelection)
		{
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
				Filter = "CS files (*.cs)|*.cs", 
				Title = "CS file to save properties to",
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

		bool FixResources()
		{
			var dict = new Dictionary<string, bool>();
			foreach (var res in _allResources)
			{
				res.ConstantName = res.ConstantName.Trim();

				if (res.ConstantName == string.Empty)
				{
					ShowError("Empty property name found. Make sure to set property names for all resources.");
					return false;
				}

				if (dict.ContainsKey(res.ConstantName))
				{
					ShowError("Duplicate property name '{0}'".Put(res.ConstantName));
					return false;
				}

				dict[res.ConstantName] = true;

				var rusTrim = res.RusString.Trim();
				var engTrim = res.EngString.Trim();

				if (rusTrim == string.Empty && engTrim == string.Empty)
				{
					ShowError("Empty resource '{0}'".Put(res.ConstantName));
					return false;
				}

				if(rusTrim == string.Empty)
					res.RusString = res.EngString;
				else if(engTrim == string.Empty)
					res.EngString = res.RusString;
			}

			return true;
		}

		const string NameSuffix = "Key";

		bool SaveCS()
		{
			if (CsFilePath.IsEmpty() || CsvFilePath.IsEmpty())
			{
				ShowError("You must first load workspace and specify CSV and CS files.");
				return false;
			}

			if (_allResources.IsEmpty())
			{
				ShowError("Resource list is empty.");
				return false;
			}

			var nspace = _tbNamespace.Text.Trim();
			if (nspace.IsEmpty())
			{
				ShowError("You must specify namespace for generated file.");
				return false;
			}

			if(!FixResources())
				return false;

			string template;

			using(var stream = GetType().Assembly.GetManifestResourceStream(GetType(), "LocalizedStrings_Template.txt"))
			using(var reader = new StreamReader(stream))
				template = reader.ReadToEnd();

			template = template.Replace("%NAMESPACE%", nspace);
			template = template.Replace("%CSV_FILENAME%", Path.GetFileName(CsvFilePath));

			var builder = new StringBuilder();
			var docBuilder = new StringBuilder();

			foreach (var res in _allResources)
			{
				var comment = res.EngString
					.Replace("&", "&amp;")
					.Replace("<", "&lt;")
					.Replace(">", "&gt;")
					.Replace("\r\n", " ")
					.Replace("\r", " ")
					.Replace("\n", " ")
					.Trim();

				docBuilder.Clear();
				docBuilder.Append("\t\t/// <summary>").AppendLine();
				docBuilder.Append("\t\t/// {0}".Put(comment)).AppendLine();
				docBuilder.Append("\t\t/// </summary>").AppendLine();

				var doc = docBuilder.ToString();

				builder.AppendLine();
				builder.Append(doc);
				builder.Append("\t\tpublic const string {0}{1} = \"{0}\";".Put(res.ConstantName, NameSuffix)).AppendLine();
				builder.AppendLine();
				builder.Append(doc);
				builder.Append("\t\tpublic static string {0}".Put(res.ConstantName)).AppendLine();
				builder.Append("\t\t{").AppendLine();
				builder.Append("\t\t\tget { return GetString("+ res.ConstantName + NameSuffix + "); }").AppendLine();
				builder.Append("\t\t}").AppendLine();
			}

			template = template.Replace("%PROPERTIES%", builder.ToString());

			File.WriteAllText(CsFilePath, template);

			MessageBox.Show(this, "Successfully saved {0} properties to {1}".Put(_allResources.Count, CsFilePath), "Saved CS File", MessageBoxButton.OK, MessageBoxImage.Information);

			return true;
		}

		bool SaveCSV()
		{
			if (CsvFilePath.IsEmpty())
			{
				ShowError("You must first load workspace and specify CSV file.");
				return false;
			}

			if (_allResources.IsEmpty())
			{
				ShowError("Resource list is empty.");
				return false;
			}

			if(!FixResources())
				return false;

			using(var writer = new CsvFileWriter(CsvFilePath))
				foreach (var res in _allResources)
					writer.WriteRow(new List<string> {res.ConstantName, res.EngString, res.RusString});

			MessageBox.Show(this, "Successfully saved {0} resources to {1}".Put(_allResources.Count, CsvFilePath), "Saved", MessageBoxButton.OK, MessageBoxImage.Information);

			return true;
		}

		string ChosenFilename() { return "chosen_strings_{0}.txt".Put(GetStringHash(_solution.FilePath).Substring(0, 6)); }

		private void RestoreSavedChosen()
		{
			var filename = ChosenFilename();

			if(!File.Exists(filename))
				return;

			var savedIds = File.ReadAllLines(filename).ToHashSet();
			var literals = _allLiterals.Where(l => savedIds.Contains(l.LiteralId)).ToList();

			foreach (var l in literals)
			{
				_allLiterals.Remove(l);
				_chosenLiterals.Add(l);
			}
		}

		void SaveChosenLiterals()
		{
			if(_solution == null)
				return;

			File.WriteAllLines(ChosenFilename(), _chosenLiterals.Select(l => l.LiteralId));
		}

		readonly Regex _isXaml = new Regex("\\.xaml$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

		private string[] GetXamlList()
		{
			if(_solution == null)
				return new string[0];

			var list = new List<string>();

			foreach (var p in _solution.Projects)
			{
				var doc = new XmlDocument();
				doc.Load(p.FilePath);
				var root = doc.DocumentElement;
				if (root == null) continue;

				var nodes = root.SelectNodes("//@Include");
				if (nodes == null) continue;

				var path = Path.GetDirectoryName(p.FilePath) + Path.DirectorySeparatorChar;

				list.AddRange(nodes.Cast<XmlAttribute>().Where(a => _isXaml.IsMatch(a.Value) && File.Exists(path + a.Value)).Select(a => path + a.Value));
			}

			return list.ToArray();
		}

		private IEnumerable<XamlResource> GetRussianStrings(XmlNode node) {
			var result = Enumerable.Empty<XamlResource>();

			if(node.Attributes != null)
				result = result
					.Concat(node.Attributes.Cast<XmlAttribute>()
						.Where(attr => _regExRussianText.IsMatch(attr.Value))
						.Select(attr => new XamlResource(node.Name, attr.Name, attr.InnerXml)));

			foreach(var child in node.ChildNodes.Cast<XmlNode>()) {
				if(child.NodeType == XmlNodeType.Text) {
					if(_regExRussianText.IsMatch(child.Value))
						result = result.Concat(new[] {new XamlResource(node.Name, null, child.OuterXml)});
				} else {
					result = result.Concat(GetRussianStrings(child));
				}
			}

			return result;
		}


		static readonly Regex _xamlNsRegex = new Regex(@"^(\s+)xmlns:\w+.*?$", RegexOptions.Multiline | RegexOptions.Compiled);

		private string AddXamlNamespace(string allText)
		{
			var match = _xamlNsRegex.Match(allText);
			if(!match.Success)
				return null;

			return _xamlNsRegex.Replace(allText, m => m.Value + m.Groups[1] + "xmlns:loc=\"clr-namespace:StockSharp.Localization;assembly=StockSharp.Localization\"", 1);
		}

		#endregion
	}

	class MyRewriter : CSharpSyntaxRewriter
	{
		readonly Dictionary<SyntaxNode, SyntaxNode> _replaceDict; 

		public int NumReplacedLiterals {get; private set;}
		public int NumReplacedNames {get; private set;}

		public MyRewriter(Dictionary<SyntaxNode, SyntaxNode> dict)
		{
			_replaceDict = dict;
		}

		public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
		{
			var replacement = _replaceDict.TryGetValue(node);

			if (replacement != null)
			{
				++NumReplacedLiterals;
				return replacement;
			}

			return base.VisitLiteralExpression(node);
		}

		public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
		{
			var replacement = _replaceDict.TryGetValue(node);

			if (replacement != null)
			{
				++NumReplacedNames;
				return replacement;
			}

			return base.VisitIdentifierName(node);
		}
	}
}
