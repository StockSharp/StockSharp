using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Ecng.Common;

namespace CsLocTool {
	/// <summary>
	/// Interaction logic for FileFilterWindow.xaml
	/// </summary>
	public partial class FileFilterWindow : Window {
		string[] _strings = new string[0];

		string _solutionName;
		bool _loaded;

		public FileFilterWindow() {
			InitializeComponent();

			Loaded += (sender, args) =>
			{
				_tb.Text = _strings.Join("\n");
				_loaded = true;
			};
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if(_solutionName.IsEmpty())
				return;

			_strings = Regex.Split(_tb.Text, @"[\r\n]+").Where(s => !s.Trim().IsEmpty()).ToArray();
			_tb.Text = _strings.Join("\n");

			File.WriteAllLines(GetFilename(_solutionName), _strings);

			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			_strings = LoadFile();
			_tb.Text = _strings.Join("\n");
		}

		public void ReloadFilters(string solution)
		{
			_solutionName = solution;
			_strings = LoadFile();
			if(_loaded)
				_tb.Text = _strings.Join("\n");

		}

		public Regex[] GetFilesFilter()
		{
			return (from s in _strings where !s.Trim().IsEmpty() 
					select new Regex(s, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToArray();
		}

		static string GetFilename(string solutionName)
		{
			return "file_filters_{0}.txt".Put(MainWindow.GetStringHash(solutionName).Substring(0, 6));
		}

		string[] LoadFile()
		{
			var fname = GetFilename(_solutionName);

			if (!File.Exists(fname))
				return new string[0];

			return File.ReadAllLines(fname);
		}
	}
}
