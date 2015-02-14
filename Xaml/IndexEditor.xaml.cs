namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Threading;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	using Expression = NCalc.Expression;

	using StockSharp.Localization;

	/// <summary>
	/// Графический контрол для редактирования <see cref="ExpressionIndexSecurity"/>.
	/// </summary>
	[CLSCompliant(false)]
	public partial class IndexEditor
	{
		private sealed class SecurityList : SynchronizedSet<Security>
		{
			private readonly IndexEditor _parent;

			public SecurityList(IndexEditor parent)
			{
				_parent = parent;
			}

			protected override void OnAdded(Security item)
			{
				_parent._securityCache.Add(item.Id, item);
				base.OnAdded(item);
			}

			protected override void OnRemoved(Security item)
			{
				_parent._securityCache.Remove(item.Id);
				base.OnRemoved(item);
			}

			protected override void OnCleared()
			{
				_parent._securityCache.Clear();
				base.OnCleared();
			}
		}

		private readonly DispatcherTimer _dispatcherTimer = new DispatcherTimer();
		private bool _popupStateBeforeWindowDeactivation;

		private readonly char[] _math = { '+', '-', ' ', '*', '/', '%', '^', '(', ')', '=', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '[' };
		private readonly char[] _mathNums = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		private readonly Dictionary<string, Security> _securityCache = new Dictionary<string, Security>(StringComparer.InvariantCultureIgnoreCase);

		private bool _validating;

		private readonly AutoCompletePopUp _ppBox = new AutoCompletePopUp();

		/// <summary>
		/// Создать <see cref="IndexEditor"/>.
		/// </summary>
		public IndexEditor()
		{
			InitializeComponent();

			// выключаем text wrapping
			//txInput.Document = new FlowDocument { PageWidth = 65535 };

			txInput.Document = new FlowDocument();

			Securities = new SecurityList(this);

			Loaded += IndexEditor_Loaded;

			_ppBox.SecuritySelected += ppBox_SecuritySelected;
			_ppBox.MatchKeyDown += key =>
			{
				switch (key)
				{
					case Key.Back:
						txInput.Focus();
						break;
					case Key.Return:
						if (_popupStateBeforeWindowDeactivation)
							_popupStateBeforeWindowDeactivation = false;

						txInput.Focus();
						break;
				}
			};

			//_ppBox.CustomPopupPlacementCallback = Repositioning;

			//DataObject.AddPastingHandler(txInput, MyPasteCommand);
			DataObject.AddCopyingHandler(txInput, MyCopyCommand);
		}

		/// <summary>
		/// Все доступные инструменты.
		/// </summary>
		public IList<Security> Securities { get; private set; }

		/// <summary>
		/// Математическая формула.
		/// </summary>
		public Expression Expression
		{
			get { return new Expression(ExpressionHelper.Encode(Text)); }
		}

		private string _text = string.Empty;

		/// <summary>
		/// Текст.
		/// </summary>
		public string Text
		{
			get
			{
				if (_validating)
				{
					return _text;
				}

				//var allTextRange = new TextRange(txInput.Document.ContentStart, txInput.Document.ContentEnd);
				var sb = new StringBuilder();
				foreach (var block in txInput.Document.Blocks)
				{
					if (block is Paragraph)
					{
						FillText(sb, LogicalTreeHelper.GetChildren(block).Cast<object>());
					}
				}

				// .Replace(" ", "")
				_text = sb.ToString();

				return _text;
			}
			set
			{
				if (txInput.Document == null)
				{
					txInput.Document = new FlowDocument();
				}
				else
				{
					txInput.Document.Blocks.Clear();
				}

				txInput.Document.Blocks.Add(new Paragraph(new Run(value)));
				Validate();
			}
		}

		/// <summary>
		/// Выражение содержит ошибку.
		/// </summary>
		public bool HasError { get; private set; }

		private readonly IList<string> _highligthFunctions = new List<String>
		{
			"abs",
			"acos",
			"asin",
			"atan",
			"ceiling",
			"cos",
			"exp",
			"floor",
			"ieeeremainder",
			"log",
			"log10",
			"max",
			"min",
			"pow",
			"round",
			"sign",
			"sin",
			"sqrt",
			"tan",
			"truncate"
		};

		/// <summary>
		/// Список подсвечиваемых функций.
		/// </summary>
		/// <remarks>
		/// По умолчанию содержит: abs, acos, asin, atan, ceiling, cos, exp, floor, ieeeremainder, log, log10, max, min, pow, round, sign, sin, sqrt, tan, truncate.
		/// </remarks>
		public IList<string> HighligthFunctions
		{
			get { return _highligthFunctions; }
		}

		private Brush _highligthForeColor = Brushes.Red;

		/// <summary>
		/// Цвет шрифта функций. По умолчанию равен <see cref="Brushes.Red"/>.
		/// </summary>
		public Brush HighligthForeColor
		{
			get { return _highligthForeColor; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_highligthForeColor = value;
			}
		}

		private Brush _highligthBackColor = Brushes.White;

		/// <summary>
		/// Цвет фона функций. По умолчанию равен <see cref="Brushes.White"/>.
		/// </summary>
		public Brush HighligthBackColor
		{
			get { return _highligthBackColor; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_highligthBackColor = value;
			}
		}

		private void MyCopyCommand(object sender, DataObjectEventArgs e)
		{
			e.CancelCommand();

			var selection = txInput.Selection;
			var navigator = selection.Start.GetPositionAtOffset(0, LogicalDirection.Forward);
			var end = selection.End;
			var buffer = new StringBuilder();

			int offset;
			do
			{
				//Debug.Assert(navigator != null, "navigator != null");
				offset = navigator.GetOffsetToPosition(end);
				var context = navigator.GetPointerContext(LogicalDirection.Forward);
				if (context == TextPointerContext.Text)
				{
					buffer.Append(
						navigator.GetTextInRun(LogicalDirection.Forward),
						0,
						Math.Min(offset, navigator.GetTextRunLength(LogicalDirection.Forward)));
				}
				else if (context == TextPointerContext.EmbeddedElement)
				{
					var link = navigator.Parent as SecurityHyperLink;

					if (link != null)
						buffer.Append(link.Security.Id);
				}
				navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
			}
			while (offset > 0);

			Clipboard.SetText(buffer.ToString());
		}

		private void IndexEditor_Loaded(object sender, RoutedEventArgs e)
		{
			var parentWindow = FindParentWindow(this);

			if (parentWindow == null)
				return;

			parentWindow.Deactivated += (c, v) => ParentDeactivated();
			parentWindow.Activated += (c, v) => ParentActivated();
			parentWindow.StateChanged += ParentWindowStateChanges;
		}

		private bool _minimized;

		private void ParentWindowStateChanges(object sender, EventArgs e)
		{
			var win = sender as Window;

			if (win == null)
				return;

			switch (win.WindowState)
			{
				case WindowState.Normal:
					ParentActivated();
					if (!_minimized)
					{
						_dispatcherTimer.Tick += dispatcherTimer_Tick;
						_dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
						_dispatcherTimer.Start();
					}
					break;
				case WindowState.Minimized:
					_minimized = true;
					ParentDeactivated();
					break;
				case WindowState.Maximized:
					_dispatcherTimer.Tick += dispatcherTimer_Tick;
					_dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
					_dispatcherTimer.Start();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void dispatcherTimer_Tick(object sender, EventArgs e)
		{
			_dispatcherTimer.Stop();
			_dispatcherTimer.Tick -= dispatcherTimer_Tick;
			_dispatcherTimer.Tick += dispatcherTimer_Tick2;

			_ppBox.IsOpen = false;

			_dispatcherTimer.Start();
		}

		private void dispatcherTimer_Tick2(object sender, EventArgs e)
		{
			_dispatcherTimer.Stop();
			_dispatcherTimer.Tick -= dispatcherTimer_Tick2;
			_ppBox.IsOpen = true;
		}

		// Methods

		private void FillMatchList()
		{
			_ppBox.Securities.Clear();

			if (txInput.CaretPosition.Parent.GetType() != typeof(Run))
			{
				return;
			}

			var txt = txInput.CaretPosition.GetTextInRun(LogicalDirection.Backward);
			var sb = new StringBuilder();
			var paragraph = txInput.CaretPosition.Paragraph;
			if (paragraph != null)
			{
				FillText(sb, paragraph.Inlines);
			}

			var ind = txt.LastIndexOfAny(_math);
			var indNums = txt.LastIndexOfAny(_mathNums);

			if (txt.IsEmpty() || (ind == indNums && ind > -1 && indNums > 0))
			{
				return;
			}

			if (ind > -1)
			{
				txt = txt.Substring(ind + 1, txt.Length - 1 - ind);
			}

			var sl = txt.ToLowerInvariant().Replace("\r", "").Replace("\n", "").Split(_math, StringSplitOptions.None);

			_ppBox.MatchText = txt;

			foreach (var security in Securities)
			{
				if (sl.Any(s => security.Id.ContainsIgnoreCase(s) && security.Id.ToLowerInvariant() != s))
					_ppBox.Securities.Add(security);
			}
		}

		private static Window FindParentWindow(DependencyObject child)
		{
			var parent = VisualTreeHelper.GetParent(child);

			//CHeck if this is the end of the tree 
			if (parent == null)
			{
				return null;
			}

			var parentWindow = parent as Window;
			if (parentWindow != null)
			{
				return parentWindow;
			}

			//use recursion until it reaches a Window 
			return FindParentWindow(parent);
		}

		private static void FillText(StringBuilder builder, IEnumerable<object> childs)
		{
			foreach (var child in childs)
			{
				var link = child as SecurityHyperLink;
				if (link != null)
				{
					builder.Append(link.Security.Id);
				}
				else
				{
					var run = child as Run;
					if (run != null)
					{
						builder.Append(run.Text);
					}
				}
			}
		}

		private void ppBox_SecuritySelected()
		{
			string selectedSec;

			if (_ppBox.SelectedSecurity != null)
				selectedSec = _ppBox.SelectedSecurity.Id;
			else
				return;

			txInput.Focus();

			var sl1 = Text.ToLower();
			var start = sl1.Length - 1;
			var caretIndex = sl1.Length;

			var end = start - (sl1.Length - caretIndex);

			var sl2 = sl1.LastIndexOfAny(_math, start, end);

			int removechartCount;
			if (sl2 < 0)
			{
				removechartCount = txInput.CaretPosition.GetTextInRun(LogicalDirection.Backward).Length;

				if (removechartCount > 0)
					txInput.CaretPosition.DeleteTextInRun(-removechartCount);

				txInput.CaretPosition.InsertTextInRun(selectedSec);
			}
			else
			{
				var cpt = txInput.CaretPosition.GetTextInRun(LogicalDirection.Backward);

				if (cpt.Length == 0)
				{
					txInput.CaretPosition.InsertTextInRun(selectedSec);
				}
				else
				{
					if (cpt.Length == 1 && _math.Contains(cpt[0]))
					{
						txInput.CaretPosition.InsertTextInRun(selectedSec);
					}
					else
					{
						var lio = cpt.LastIndexOfAny(_math);
						if (lio >= 0)
						{
							var part = cpt.Substring(lio + 1);
							var found = _securityCache.Keys.Select(c => c.ToLower().StartsWith(part)).Any();
							removechartCount = part.Length;

							if (found && removechartCount > 0)
							{
								txInput.CaretPosition.DeleteTextInRun(removechartCount * -1);
							}

							txInput.CaretPosition.InsertTextInRun(selectedSec);
						}
						else
						{
							var found = _securityCache.Keys.Select(c => c.ToLower().StartsWith(cpt)).Any();
							removechartCount = cpt.Length;

							if (found)
							{
								txInput.CaretPosition.DeleteTextInRun(removechartCount * -1);
							}

							txInput.CaretPosition.InsertTextInRun(selectedSec);
						}
					}
				}
			}

			_ppBox.IsOpen = false;

			if (_popupStateBeforeWindowDeactivation)
				_popupStateBeforeWindowDeactivation = false;

			var tp = txInput.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);

			txInput.CaretPosition = tp.GetPositionAtOffset(selectedSec.Length) ?? tp;
		}

		/// <summary>
		/// Событие клика на инструмент.
		/// </summary>
		public event Action<Security> SecurityClicked;

		private sealed class SecurityHyperLink : Hyperlink
		{
			public SecurityHyperLink(Security security, TextBlock tb)
				: base(new InlineUIContainer(tb) { Cursor = Cursors.Hand })
			{
				if (security == null)
					throw new ArgumentNullException("security");

				Security = security;
				Cursor = Cursors.Hand;
			}

			public Security Security { get; private set; }
		}

		private SecurityHyperLink CreateSecurityLink(Security security)
		{
			var tb = new TextBlock { TextDecorations = TextDecorations.Underline, Text = security.Id, Tag = security };
			tb.MouseLeftButtonDown += (o, rr) =>
			{
				var clicked = SecurityClicked;

				if (clicked == null)
					return;

				var textBlock = o as TextBlock;

				if (textBlock != null)
					clicked((Security)textBlock.Tag);
			};

			return new SecurityHyperLink(security, tb);
		}

		private void Validate()
		{
			_validating = true;

			try
			{
				if (!Text.IsEmpty())
				{
					var expression = Expression;

					if (expression.HasErrors())
					{
						ShowErrorBorder(expression.Error);
					}
					else
					{
						var securities = new List<Security>();

						var r = new Regex(@"[^\[]*\[([^\]]+)]");
						var matches = r.Matches(expression.ParsedExpression.ToString());

						foreach (Match match in matches)
						{
							var m = match.Groups[1].ToString();

							var security = _securityCache.TryGetValue(m);
							if (security == null)
							{
								ShowErrorBorder(LocalizedStrings.Str1522Params.Put(m));
								return;
							}

							securities.Add(security);
						}

						ShowErrorBorder(null);

						RebuildContainer(securities);
					}
				}
				else
				{
					ShowErrorBorder(null);
				}
			}
			finally
			{
				_validating = false;
			}
		}

		private Brush _normalBrush;

		private void ShowErrorBorder(string errorText)
		{
			if (_normalBrush == null)
				_normalBrush = txInput.BorderBrush;

			HasError = errorText != null;

			txInput.BorderBrush = HasError ? Brushes.Red : _normalBrush;
			txInput.ToolTip = errorText;
		}

		private void RebuildContainer(IEnumerable<Security> securities)
		{
			var slim = Text;

			var paragraph = (Paragraph)txInput.Document.Blocks.First();

			var index = 0;

			foreach (var security in securities)
			{
				var objind = slim.IndexOf(security.Id, StringComparison.InvariantCultureIgnoreCase);
				var after = security.Id.Length; // +2;
				var prevstr = slim.Substring(0, objind);
				slim = slim.Substring(objind + after, slim.Length - (objind + after));

				if (!prevstr.IsEmpty())
					index = RebuildFunctionsNames(paragraph, prevstr, index);

				if (paragraph.Inlines.Count <= index ||
					!(paragraph.Inlines.ElementAt(index) is SecurityHyperLink) ||
					((SecurityHyperLink)paragraph.Inlines.ElementAt(index)).Security != security)
				{
					if (paragraph.Inlines.Count <= index)
						paragraph.Inlines.Add(CreateSecurityLink(security));
					else
						paragraph.Inlines.InsertBefore(paragraph.Inlines.ElementAt(index), CreateSecurityLink(security));
				}

				index++;
			}

			if (!slim.IsEmpty())
			{
				index = RebuildFunctionsNames(paragraph, slim, index);
			}

			while (paragraph.Inlines.Count > index)
				paragraph.Inlines.Remove(paragraph.Inlines.Last());
		}

		private int RebuildFunctionsNames(Paragraph paragraph, string prevstr, int index)
		{
			while (true)
			{
				const string prepareRegex = "(^.*?)(({0})+)(\\([^\\(\\)]*)(.*)";
				var regPattern = string.Format(prepareRegex, HighligthFunctions.Aggregate((a, b) => a + "|" + b));
				var r = new Regex(regPattern, RegexOptions.IgnoreCase);
				var mm = r.Matches(prevstr);
				if (mm.Count > 0)
				{
					var groups = mm[0].Groups;
					var before = groups[1].ToString();
					index = TryAddRun(paragraph, index, new Run(before));
					var func = groups[2].ToString();
					index = TryAddRun(paragraph, index, new Run(func) { Background = HighligthBackColor, Foreground = HighligthForeColor });
					index = TryAddRun(paragraph, index, new Run(string.Empty));
					var funcParam = groups[4].ToString();
					index = TryAddRun(paragraph, index, new Run(funcParam));

					prevstr = prevstr.Replace(before + func + funcParam, "");
				}
				else
				{
					index = TryAddRun(paragraph, index, new Run(prevstr));
					break;
				}
			}

			return index;
		}

		private static int TryAddRun(Paragraph paragraph, int index, Run run)
		{
			if (paragraph.Inlines.Count <= index ||
				!(paragraph.Inlines.ElementAt(index) is Run) ||
				((Run)paragraph.Inlines.ElementAt(index)).Text != run.Text)
			{
				if (paragraph.Inlines.Count <= index)
					paragraph.Inlines.Add(run);
				else
					paragraph.Inlines.InsertBefore(paragraph.Inlines.ElementAt(index), run);
			}

			return index + 1;
		}

		private void ParentActivated()
		{
			if (!_popupStateBeforeWindowDeactivation)
				return;

			if (Text.Length == 0)
			{
				ShutPopupForEmptyTextBox();
			}
			else
			{
				var parentWindow = FindParentWindow(this);
				if (parentWindow != null)
				{
					if (parentWindow.WindowState == WindowState.Minimized)
						return;
				}

				FillMatchList();

				if (!_ppBox.Securities.IsEmpty())
				{
					ShowAutoComplete();
				}
				else
				{
					ShutPopupForEmptyTextBox();
				}
			}
		}

		private void ShowAutoComplete()
		{
			_ppBox.PlacementTarget = txInput;
			_ppBox.PlacementRectangle = txInput.CaretPosition.GetCharacterRect(LogicalDirection.Backward);
			_ppBox.IsOpen = true;
		}

		private void ParentDeactivated()
		{
			if (!_ppBox.IsOpen)
				return;

			_popupStateBeforeWindowDeactivation = true;
			_ppBox.IsOpen = false;
		}

		private void ShutPopupForEmptyTextBox()
		{
			if (!_ppBox.IsOpen)
				return;

			_ppBox.IsOpen = false;
			_popupStateBeforeWindowDeactivation = false;
		}

		private void txInput_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (_validating)
			{
				return;
			}

			switch (e.Key)
			{
				case Key.Back:
					if (Text.Length == 1)
					{
						ShutPopupForEmptyTextBox();
					}
					break;

				case Key.Down:
					_ppBox.DoFocus();
					break;

				case Key.Tab:
					if (_ppBox.IsOpen)
					{
						_ppBox.DoFocus();
					}
					break;

				case Key.Escape:
					ShutPopupForEmptyTextBox();
					break;
				case Key.Enter:
					if (!_ppBox.Securities.IsEmpty())
					{
						_ppBox.SelectedSecurity = _ppBox.Securities.First();
						ppBox_SecuritySelected();
					}
					break;
			}
		}

		/// <summary>
		/// Событие изменения <see cref="Text"/>.
		/// </summary>
		public event TextChangedEventHandler TextChanged;

		private void txInput_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_validating)
			{
				return;
			}

			TextChanged.Cast<TextChangedEventArgs>().SafeInvoke(sender, e);

			//if (txInput.Document.Blocks.IsEmpty())
			if (Text.IsEmpty())
			{
				ShutPopupForEmptyTextBox();
				Validate();
			}
			else
			{
				FillMatchList();
				Validate();

				if (!_ppBox.Securities.IsEmpty())
				{
					ShowAutoComplete();
				}
				else
				{
					ShutPopupForEmptyTextBox();
				}
			}
		}

		private void UserControl_LostFocus(object sender, RoutedEventArgs e)
		{
			if (_validating)
				return;

			Validate();
			_validating = false;
		}
	}
}