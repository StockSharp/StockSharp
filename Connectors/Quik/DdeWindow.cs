#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: DdeWindow.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik
{
	using System;
	using System.Windows.Forms;

	using Ecng.Common;
	using Ecng.Interop;

	using ManagedWinapi.Windows;

	/// <summary>
	/// Окно DDE экспорта.
	/// </summary>
	sealed class DdeWindow : Equatable<DdeWindow>
	{
		private readonly SystemWindow _tableName;
		private readonly SystemWindow _ddeServer;
		private readonly SystemWindow _book;
		private readonly SystemWindow _sheet;
		private readonly SystemWindow _row;
		private readonly SystemWindow _column;
		private readonly SystemWindow _startFromRow;
		private readonly SystemWindow _outAfterCreate;
		private readonly SystemWindow _outAfterCtrlShiftL;
		private readonly SystemWindow _rowsCaption;
		private readonly SystemWindow _columnsCaption;
		private readonly SystemWindow _formalValues;
		private readonly SystemWindow _emptyCells;

		internal DdeWindow(SystemWindow wnd)
		{
			if (wnd == null)
				throw new ArgumentNullException(nameof(wnd));

			Window = wnd;

			foreach (var childWnd in wnd.AllChildWindows)
			{
				switch (childWnd.DialogID)
				{
					case 0x29cd:
						_tableName = childWnd;
						break;

					case 0x29d8:
						_ddeServer = childWnd;
						break;

					case 0x29ce:
						_book = childWnd;
						break;

					case 0x29cf:
						_sheet = childWnd;
						break;

					case 0x29d0:
						_row = childWnd;
						break;

					case 0x29d1:
						_column = childWnd;
						break;

					case 0x29d2:
						BeginOutBtn = childWnd;
						break;

					case 0x29d3:
						StopOutBtn = childWnd;
						break;

					case 0x29d5:
						_rowsCaption = childWnd;
						break;

					case 0x29d6:
						_columnsCaption = childWnd;
						break;

					case 0x29d7:
						_formalValues = childWnd;
						break;

					case 0x29d9:
						_emptyCells = childWnd;
						break;

					case 0x29da:
						_outAfterCreate = childWnd;
						break;

					case 0x29db:
						_startFromRow = childWnd;
						break;

					case 0x29dc:
						_outAfterCtrlShiftL = childWnd;
						break;

					case 1:
						CloseBtn = childWnd;
						break;
				}
			}
		}

		public string DdeServer
		{
			get { return _ddeServer.GetText(); }
			set { _ddeServer.SetText(value); }
		}

		public string Book
		{
			get { return _book.GetText(); }
			set { _book.SetText(value); }
		}

		public string Sheet
		{
			get { return _sheet.GetText(); }
			set { _sheet.SetText(value); }
		}

		public int Row
		{
			get { return _row.GetText().To<int>(); }
			set { _row.SetText(value.ToString()); }
		}

		public int Column
		{
			get { return _column.GetText().To<int>(); }
			set { _column.SetText(value.ToString()); }
		}

		public int StartFromRow
		{
			get { return _startFromRow.GetText().To<int>(); }
			set { _startFromRow.SetText(value.ToString()); }
		}

		public bool OutAfterCreate
		{
			get { return _outAfterCreate.CheckState == CheckState.Checked; }
			set { _outAfterCreate.CheckState = value ? CheckState.Checked : CheckState.Unchecked; }
		}

		public bool OutAfterCtrlShiftL
		{
			get { return _outAfterCtrlShiftL.CheckState == CheckState.Checked; }
			set { _outAfterCtrlShiftL.CheckState = value ? CheckState.Checked : CheckState.Unchecked; }
		}

		public bool RowsCaption
		{
			get { return _rowsCaption.CheckState == CheckState.Checked; }
			set { _rowsCaption.CheckState = value ? CheckState.Checked : CheckState.Unchecked; }
		}

		public bool ColumnsCaption
		{
			get { return _columnsCaption.CheckState == CheckState.Checked; }
			set { _columnsCaption.CheckState = value ? CheckState.Checked : CheckState.Unchecked; }
		}

		public bool FormalValues
		{
			get { return _formalValues.CheckState == CheckState.Checked; }
			set { _formalValues.CheckState = value ? CheckState.Checked : CheckState.Unchecked; }
		}

		public bool EmptyCells
		{
			get { return _emptyCells.CheckState == CheckState.Checked; }
			set { _emptyCells.CheckState = value ? CheckState.Checked : CheckState.Unchecked; }
		}

		internal SystemWindow BeginOutBtn { get; }

		internal SystemWindow StopOutBtn { get; }

		internal SystemWindow CloseBtn { get; }

		/// <summary>
		/// Описание окна DDE.
		/// </summary>
		internal SystemWindow Window { get; }

		public override DdeWindow Clone()
		{
			return new DdeWindow(Window);
		}

		/// <summary>
		/// Проверить на эквивалентность с другим окном.
		/// </summary>
		/// <param name="other">Другое окно.</param>
		/// <returns><see langword="true"/>, если окна эквивалентны. Иначе, <see langword="false"/>.</returns>
		protected override bool OnEquals(DdeWindow other)
		{
			return Window == other.Window;
		}

		/// <summary>
		/// Получить хеш-код.
		/// </summary>
		/// <returns>Хеш-код</returns>
		public override int GetHashCode()
		{
			return Window.GetHashCode();
		}
	}
}