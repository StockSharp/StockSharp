namespace StockSharp.Algo.Export;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DevExpress.Export.Xl;

using Ecng.Collections;
using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Interop;

/// <summary>
/// Implementation of the <see cref="IExcelWorkerProvider"/> works with DevExpress excel processors.
/// </summary>
public class DevExpExcelWorkerProvider : IExcelWorkerProvider
{
	private class DevExpExcelWorker : IExcelWorker
	{
		private class SheetData : IDisposable
		{
			private readonly DevExpExcelWorker _worker;
			private readonly Dictionary<int, SortedDictionary<int, object>> _cells = [];

			public readonly SortedDictionary<int, RefPair<Type, string>> Columns = [];
			public readonly HashSet<int> Rows = [];

			public SheetData(DevExpExcelWorker worker)
			{
				_worker = worker ?? throw new ArgumentNullException(nameof(worker));
			}

			public string Name { get; set; }

			public void SetCell<T>(int col, int row, T value)
			{
				Columns.TryAdd(col, new RefPair<Type, string>());
				Rows.Add(row);
				_cells.SafeAdd(row, key => [])[col] = value;
			}

			public T GetCell<T>(int col, int row)
			{
				return (T)_cells.SafeAdd(row, key => []).TryGetValue(col);
			}

			public void Dispose()
			{
				using (var sheet = _worker._document.CreateSheet())
				{
					if (!Name.IsEmpty())
						sheet.Name = Name;

					foreach (var pair in Columns)
					{
						if (pair.Value.First != null)
						{

						}
						else if (!pair.Value.Second.IsEmpty())
						{
							using var xlCol = sheet.CreateColumn(pair.Key);

							xlCol.Formatting = new XlCellFormatting
							{
								IsDateTimeFormatString = true,
								NetFormatString = pair.Value.Second,
							};
						}
					}

					foreach (var row in Rows.OrderBy())
					{
						if (!_cells.TryGetValue(row, out var dict))
							continue;

						using var xlRow = sheet.CreateRow(row);

						foreach (var pair in dict)
						{
							if (pair.Value == null)
								continue;

							XlVariantValue xlVal;

							if (pair.Value is bool b)
								xlVal = new XlVariantValue { BooleanValue = b };
							else if (pair.Value is DateTime dt)
								xlVal = new XlVariantValue { DateTimeValue = dt };
							else if (pair.Value is DateTimeOffset dto)
								xlVal = new XlVariantValue { DateTimeValue = dto.DateTime };
							//else if (pair.Value is string s)
							//	xlVal = new XlVariantValue { TextValue = s };
							else if (pair.Value.GetType().IsNumeric())
								xlVal = new XlVariantValue { NumericValue = pair.Value.To<double>() };
							//else if (typeof(T) == typeof(Exception))
							//	xlVal = new XlVariantValue { ErrorValue = new NameError() };
							else
							{
								xlVal = new XlVariantValue { TextValue = pair.Value.To<string>() };
								//throw new ArgumentOutOfRangeException(pair.Value?.ToString());
							}

							using var cell = xlRow.CreateCell(pair.Key);
							cell.Value = xlVal;
						}
					}
				}

				Columns.Clear();
				Rows.Clear();

				_cells.Clear();
			}
		}

		private readonly IXlExporter _exporter = XlExport.CreateExporter(XlDocumentFormat.Xlsx);
		private readonly IXlDocument _document;
		private readonly List<SheetData> _sheets = [];
		private SheetData _currSheet;

		public DevExpExcelWorker(Stream stream)
		{
			_document = _exporter.CreateDocument(stream);
		}

		void IDisposable.Dispose()
		{
			_sheets.ForEach(s => s.Dispose());
			_sheets.Clear();

			_document.Dispose();

			GC.SuppressFinalize(this);
		}

		IExcelWorker IExcelWorker.SetCell<T>(int col, int row, T value)
		{
			_currSheet.SetCell(col, row, value);
			return this;
		}

		T IExcelWorker.GetCell<T>(int col, int row)
		{
			return _currSheet.GetCell<T>(col, row);
		}

		IExcelWorker IExcelWorker.SetStyle(int col, Type type)
		{
			_currSheet.Columns[col] = new RefPair<Type, string>(type, null);
			return this;
		}

		IExcelWorker IExcelWorker.SetStyle(int col, string format)
		{
			_currSheet.Columns[col] = new RefPair<Type, string>(null, format);
			return this;
		}

		IExcelWorker IExcelWorker.SetConditionalFormatting(int col, ComparisonOperator op, string condition, string bgColor, string fgColor)
		{
			return this;
		}

		IExcelWorker IExcelWorker.RenameSheet(string name)
		{
			_currSheet.Name = name;
			return this;
		}

		IExcelWorker IExcelWorker.AddSheet()
		{
			_currSheet = new SheetData(this);
			_sheets.Add(_currSheet);
			return this;
		}

		bool IExcelWorker.ContainsSheet(string name) => _sheets.Any(s => s.Name.EqualsIgnoreCase(name));

		IExcelWorker IExcelWorker.SwitchSheet(string name)
		{
			_currSheet = _sheets.First(s => s.Name.EqualsIgnoreCase(name));
			return this;
		}

		int IExcelWorker.GetColumnsCount() => _currSheet.Columns.Count;
		int IExcelWorker.GetRowsCount() => _currSheet.Rows.Count;
	}

	IExcelWorker IExcelWorkerProvider.CreateNew(Stream stream, bool readOnly)
	{
		return new DevExpExcelWorker(stream);
	}

	IExcelWorker IExcelWorkerProvider.OpenExist(Stream stream)
	{
		return new DevExpExcelWorker(stream);
	}
}