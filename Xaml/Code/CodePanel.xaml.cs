namespace StockSharp.Xaml.Code
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Input;

	using ActiproSoftware.Text;
	using ActiproSoftware.Text.Languages.CSharp.Implementation;
	using ActiproSoftware.Text.Languages.DotNet;
	using ActiproSoftware.Text.Languages.DotNet.Reflection;
	using ActiproSoftware.Windows.Controls.SyntaxEditor;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// Визуальная панель для редактирования и компилирования кода.
	/// </summary>
	public partial class CodePanel : IPersistable
	{
		private class CompileResultItem
		{
			public CompilationErrorTypes Type { get; set; }
			public int Line { get; set; }
			public int Column { get; set; }
			public string Message { get; set; }
		}

		/// <summary>
		/// Команда для компиляции кода.
		/// </summary>
		public static RoutedCommand CompileCommand = new RoutedCommand();

		/// <summary>
		/// Команда для сохранения кода.
		/// </summary>
		public static RoutedCommand SaveCommand = new RoutedCommand();

		/// <summary>
		/// Команда для модификации ссылок.
		/// </summary>
		public static RoutedCommand ReferencesCommand = new RoutedCommand();

		/// <summary>
		/// Команда для отмены изменений.
		/// </summary>
		public static RoutedCommand UndoCommand = new RoutedCommand();

		/// <summary>
		/// Команда для возврата изменений.
		/// </summary>
		public static RoutedCommand RedoCommand = new RoutedCommand();

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="ShowToolBar"/>.
		/// </summary>
		public static readonly DependencyProperty ShowToolBarProperty = DependencyProperty.Register("ShowToolBar", typeof(bool), typeof(CodePanel), new PropertyMetadata(true));

		/// <summary>
		/// Показывать панель обзора.
		/// </summary>
		public bool ShowToolBar
		{
			get { return (bool)GetValue(ShowToolBarProperty); }
			set { SetValue(ShowToolBarProperty, value); }
		}

		private readonly IProjectAssembly _projectAssembly;
		private readonly ObservableCollection<CompileResultItem> _errorsSource;

		private static readonly SynchronizedDictionary<CodeReference, IProjectAssemblyReference> _references = new SynchronizedDictionary<CodeReference, IProjectAssemblyReference>();

		/// <summary>
		/// Создать <see cref="CodePanel"/>.
		/// </summary>
		public CodePanel()
		{
			InitializeComponent();

			_projectAssembly = new CSharpProjectAssembly("StudioStrategy");
			_projectAssembly.AssemblyReferences.AddMsCorLib();

			var language = new CSharpSyntaxLanguage();
			language.RegisterProjectAssembly(_projectAssembly);
			CodeEditor.Document.Language = language;

			_errorsSource = new ObservableCollection<CompileResultItem>();
			ErrorsGrid.ItemsSource = _errorsSource;

			var references = new SynchronizedList<CodeReference>();
			references.Added += r => _references.SafeAdd(r, r1 =>
			{
				IProjectAssemblyReference asmRef = null;

				try
				{
					asmRef = _projectAssembly.AssemblyReferences.AddFrom(r1.Location);
				}
				catch (Exception ex)
				{
					ex.LogError();
				}

				return asmRef;
			});
			references.Removed += r =>
			{
				var item = _projectAssembly
					.AssemblyReferences
					.FirstOrDefault(p =>
					{
						var assm = p.Assembly as IBinaryAssembly;

						if (assm == null)
							return false;

						return assm.Location == r.Location;
					});

				if (item != null)
					_projectAssembly.AssemblyReferences.Remove(item);
			};
			references.Cleared += () =>
			{
				_projectAssembly.AssemblyReferences.Clear();
				_projectAssembly.AssemblyReferences.AddMsCorLib();
			};

			References = references;
		}

		/// <summary>
		/// Код.
		/// </summary>
		public string Code
		{
			get
			{
				CodeEditor.Document.IsModified = false;
				return CodeEditor.Text;
			}
			set
			{
				CodeEditor.Text = value;
			}
		}

		/// <summary>
		/// Ссылки.
		/// </summary>
		public IList<CodeReference> References { get; private set; }

		/// <summary>
		/// Событие обновления ссылок.
		/// </summary>
		public event Action ReferencesUpdated;

		/// <summary>
		/// Событие компиляции кода.
		/// </summary>
		public event Action CompilingCode;

		/// <summary>
		/// Событие сохранения кода.
		/// </summary>
		public event Action SavingCode;

		/// <summary>
		/// Событие изменения кода.
		/// </summary>
		public event Action CodeChanged;

		/// <summary>
		/// Событие проверки возможности компиляции.
		/// </summary>
		public event Func<bool> CanCompile;

		/// <summary>
		/// Показать результат компиляции.
		/// </summary>
		/// <param name="result">Результат компиляции.</param>
		/// <param name="isRunning">Запущен ли ранее скопилированный код на исполнение в текущий момент.</param>
		public void ShowCompilationResult(CompilationResult result, bool isRunning)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			_errorsSource.Clear();

			_errorsSource.AddRange(result.Errors.Select(error => new CompileResultItem
			{
				Type = error.Type,
				Line = error.NativeError.Line,
				Column = error.NativeError.Column,
				Message = error.NativeError.ErrorText,
			}));

			if (_errorsSource.All(e => e.Type != CompilationErrorTypes.Error))
			{
				if (isRunning)
				{
					_errorsSource.Add(new CompileResultItem
					{
						Type = CompilationErrorTypes.Warning,
						Message = LocalizedStrings.Str1420
					});
				}

				_errorsSource.Add(new CompileResultItem
				{
					Type = CompilationErrorTypes.Info,
					Message = LocalizedStrings.Str1421
				});
			}
		}

		/// <summary>
		/// Редактировать ссылки.
		/// </summary>
		public void EditReferences()
		{
			var window = new CodeReferencesWindow();

			window.References.AddRange(References);

			if (!window.ShowModal(this))
				return;

			var toAdd = window.References.Except(References);
			var toRemove = References.Except(window.References);

			References.RemoveRange(toRemove);
			References.AddRange(toAdd);

			ReferencesUpdated.SafeInvoke();
		}

		private void ExecutedSaveCommand(object sender, ExecutedRoutedEventArgs e)
		{
			SavingCode.SafeInvoke();
		}

		private void CanExecuteSaveCodeCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = CodeEditor != null && CodeEditor.Document.IsModified;
		}

		private void ExecutedCompileCommand(object sender, ExecutedRoutedEventArgs e)
		{
			CompilingCode.SafeInvoke();

			//var compiler = Compiler.Create(CodeLanguage, AssemblyPath, TempPath);
			//CodeCompiled.SafeInvoke(compiler.Compile(AssemblyName, Code, References.Select(s => s.Location)));
		}

		private void ExecutedReferencesCommand(object sender, ExecutedRoutedEventArgs e)
		{
			EditReferences();
		}

		private void CanExecuteReferencesCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ErrorsGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var error = (CompileResultItem)ErrorsGrid.SelectedItem;

			if (error == null)
				return;

			CodeEditor.ActiveView.Selection.StartPosition = new TextPosition(error.Line - 1, error.Column - 1);
			CodeEditor.Focus();
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			ErrorsGrid.Load(storage.GetValue<SettingsStorage>("ErrorsGrid"));

			var layout = storage.GetValue<string>("Layout");
			if (layout != null)
				DockSite.LoadLayout(layout, true);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("ErrorsGrid", ErrorsGrid.Save());
			storage.SetValue("Layout", DockSite.SaveLayout(true));
		}

		private void CanExecuteCompileCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = CanCompile == null || CanCompile();
		}

		private void CodeEditor_OnDocumentTextChanged(object sender, EditorSnapshotChangedEventArgs e)
		{
			if (e.OldSnapshot.Text.IsEmpty())
				return;

			CodeChanged.SafeInvoke();
		}

		private void ExecutedUndoCommand(object sender, ExecutedRoutedEventArgs e)
		{
			CodeEditor.Document.UndoHistory.Undo();
		}

		private void CanExecuteUndoCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = CodeEditor != null && CodeEditor.Document.UndoHistory.CanUndo;
		}

		private void ExecutedRedoCommand(object sender, ExecutedRoutedEventArgs e)
		{
			CodeEditor.Document.UndoHistory.Redo();
		}

		private void CanExecuteRedoCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = CodeEditor != null && CodeEditor.Document.UndoHistory.CanRedo;
		}
	}
}