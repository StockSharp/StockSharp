#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.DocViewer.DocViewer
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.DocViewer
{
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Navigation;
	using File = System.IO.File;

	using Ookii.Dialogs.Wpf;

	using XMLCommToHTM;
	using XMLCommToHTM.DOM;

	public partial class MainWindow
	{
		//private readonly SiteRootObject _rootObject;
		private OfflineDynamicPage _root;

		public MainWindow()
		{
			InitializeComponent();

			//_rootObject = ConfigManager.GetService<SiteRootObject>();
		}

		private void ItemContent_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			if (e.Uri == null || e.Uri.ToString().Contains("msdn"))
				return;

			var page = FindPage(_root, e.Uri.ToString());
			e.Cancel = true;
			ItemContent.NavigateToString(page);
		}

		static string FindPage(OfflineDynamicPage p, string url)
		{
			return p.UrlPart == url
				? p.RussianContent
				: p.Childs.Select(ch => FindPage(ch, url)).FirstOrDefault(res => res != null);
		}

		private void Browse_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaOpenFileDialog
			{
				Filter = "Assembly Files (*.dll)|*.dll",
				Multiselect = true,
			};

			if (dlg.ShowDialog(this) != true)
				return;

			Toc.Items.Clear();

			var navigation = new Navigation
			{
				UrlPrefix = "http://stocksharp.com/doc/ref/",
				EmptyImage = "http://stocksharp.com/images/blank.gif"
			};
			GenerateHtml.CssUrl = @"file:///C:/VisualStudio/Web/trunk/Site/css/style.css";
			GenerateHtml.Navigation = navigation; //ToDo: переделать
			GenerateHtml.IsHtmlAsDiv = false;
			GenerateHtml.IsRussian = true;

			var asmFiles = dlg.FileNames;
			var docFiles = asmFiles
				.Select(f => Path.Combine(Path.GetDirectoryName(f), Path.GetFileNameWithoutExtension(f) + ".xml"))
				.Where(File.Exists)
				.ToArray();

			var slnDom = SolutionDom.Build("StockSharp. Описание типов", asmFiles, docFiles, Path.GetFullPath(@"..\..\..\..\..\StockSharp\trunk\Documentation\DocSandCastle\Comments\project.xml"), null, new FindOptions
			{
				InternalClasses = false,
				UndocumentedClasses = true,
				PrivateMembers = false,
				UndocumentedMembers = true
			});

			_root = BuildPages.BuildSolution(slnDom);
			BuildTree(_root, Toc.Items);
		}

		private static void BuildTree(OfflineDynamicPage rootDp, ItemCollection rootItems)
		{
			var tvItem = new TreeViewItem
			{
				Header = rootDp.RussianTitle,
				Tag = rootDp,
			};

			rootItems.Add(tvItem);

			foreach (var item in rootDp.Childs)
			{
				BuildTree(item, tvItem.Items);
			}
		}

		private void Toc_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			var item = (TreeViewItem)e.NewValue;
			ItemContent.NavigateToString(((OfflineDynamicPage)item.Tag).RussianContent);
		}
	}
}