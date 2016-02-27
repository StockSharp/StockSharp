#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Actipro.Code.Xaml.ActiproPublic
File: CodePanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml.Actipro.Code
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
	using StockSharp.Xaml.Code;

	/// <summary>
	/// The visual panel for code editing and compiling.
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
		/// The command for the code compilation.
		/// </summary>
		public static RoutedCommand CompileCommand = new RoutedCommand();

		/// <summary>
		/// The command for the code saving.
		/// </summary>
		public static RoutedCommand SaveCommand = new RoutedCommand();

		/// <summary>
		/// The command for the references modification.
		/// </summary>
		public static RoutedCommand ReferencesCommand = new RoutedCommand();

		/// <summary>
		/// The command for undo the changes.
		/// </summary>
		public static RoutedCommand UndoCommand = new RoutedCommand();

		/// <summary>
		/// The command for return the changes.
		/// </summary>
		public static RoutedCommand RedoCommand = new RoutedCommand();

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="CodePanel.ShowToolBar"/>.
		/// </summary>
		public static readonly DependencyProperty ShowToolBarProperty = DependencyProperty.Register(nameof(ShowToolBar), typeof(bool), typeof(CodePanel), new PropertyMetadata(true));

		/// <summary>
		/// To show the review panel.
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
		/// Initializes a new instance of the <see cref="CodePanel"/>.
		/// </summary>
		public CodePanel()
		{
			InitializeComponent();

			_projectAssembly = new CSharpProjectAssembly("StockSharpStrategyCode");
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
		/// Code.
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
		/// References.
		/// </summary>
		public IList<CodeReference> References { get; }

		/// <summary>
		/// The links update event.
		/// </summary>
		public event Action ReferencesUpdated;

		/// <summary>
		/// The code compilation event.
		/// </summary>
		public event Action CompilingCode;

		/// <summary>
		/// The code saving event.
		/// </summary>
		public event Action SavingCode;

		/// <summary>
		/// The code change event.
		/// </summary>
		public event Action CodeChanged;

		/// <summary>
		/// The compilation possibility check event.
		/// </summary>
		public event Func<bool> CanCompile;

		/// <summary>
		/// To show the result of the compilation.
		/// </summary>
		/// <param name="result">The result of the compilation.</param>
		/// <param name="isRunning">Whether the previously compiled code launched in the current moment.</param>
		public void ShowCompilationResult(CompilationResult result, bool isRunning)
		{
			if (result == null)
				throw new ArgumentNullException(nameof(result));

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
		/// To edit links.
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
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			ErrorsGrid.Load(storage.GetValue<SettingsStorage>("ErrorsGrid"));

			var layout = storage.GetValue<string>("Layout");
			if (layout != null)
				DockSite.LoadLayout(layout, true);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
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