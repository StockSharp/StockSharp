using System.IO;
using System.Linq;
using System.Reflection;

namespace StockSharp.Samples
{
	static class HistoryDataHelper
	{
		public static string DataPath {get;}

		static HistoryDataHelper()
		{
			// ReSharper disable once AssignNullToNotNullAttribute
			var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

			while (dir != null)
			{
				var hdRoot = FindHistoryDataSubfolder(new DirectoryInfo(Path.Combine(dir.FullName, "packages", "stocksharp.samplehistorydata")));
				if(hdRoot != null)
				{
					DataPath = hdRoot.FullName;
					return;
				}

				dir = dir.Parent;
			}
		}

		static DirectoryInfo FindHistoryDataSubfolder(DirectoryInfo packageRoot)
		{
			if(!packageRoot.Exists)
				return null;

			foreach (var di in packageRoot.GetDirectories().OrderByDescending(di => di.Name))
			{
				var d = new DirectoryInfo(Path.Combine(di.FullName, "HistoryData"));
				if(d.Exists)
					return d;
			}

			return null;
		}
	}
}