using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Win32;

namespace CsDocReplaceTool {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public static MainWindow Instance {get; private set;}
        public static string LastSelectDirectory {get; set;}

        readonly Dictionary<string, StringResource> _resourcesDict = new Dictionary<string, StringResource>();
        readonly Dictionary<string, CodeXmlDoc> _newXmlDocDict = new Dictionary<string, CodeXmlDoc>();
        readonly Dictionary<string, CodeXmlDoc> _oldXmlDocDict = new Dictionary<string, CodeXmlDoc>();

        VSSolution _solution;

        string _resourcesFile, _csvDocFile, _solutionFile;
        bool _analysisDone, _rewriteDone;

        bool DataIsLoaded {get {return _resourcesFile != null && _csvDocFile != null && _solutionFile != null;}}

        public MainWindow() {
            Instance = this;
            InitializeComponent();

            UpdateButtonsState();

            Closing += (sender, args) => {
                if(_solution != null)
                    _solution.Dispose();
            };
        }

        async void SelectResourcesCsv_Click(object sender, RoutedEventArgs e) {
            var filePath = SelectCsvFile(_tbResourcesCsvText.Text, "Выбор CSV файла ресурсов", "CSV files (*.csv)|*.csv");
            if(string.IsNullOrEmpty(filePath))
                return;

            _resourcesFile = null;
            _analysisDone = false;

            _tbResourcesCsvText.Text = filePath;
            _resourcesDict.Clear();

            await BlockInputAction(() => {
                var csv = ReadCsvFile(filePath, 3);
                foreach(var arr in csv) {
                    var key = arr[0].Trim();
                    _resourcesDict.Add(key, new StringResource(key, arr[1], arr[2]));
                }

                if(_resourcesDict.Count > 0)
                    _resourcesFile = filePath;

                return Task.FromResult(0);
            }, "Загрузка CSV ресурсов...");
        }

        async void SelectDocCsv_Click(object sender, RoutedEventArgs e) {
            var filePath = SelectCsvFile(_tbDocDsvText.Text, "Выбор CSV файла со структурой XML документации", "CSV files (*.csv)|*.csv");
            if(string.IsNullOrEmpty(filePath))
                return;

            _csvDocFile = null;
            _analysisDone = false;

            _tbDocDsvText.Text = filePath;
            _newXmlDocDict.Clear();

            await BlockInputAction(() => {
                var csv = ReadCsvFile(filePath, 2);
                foreach(var arr in csv) {
                    var docPartId = arr[0].Trim();
                    var slashIndex = docPartId.IndexOf('/');

                    if(slashIndex < 0) {
                        Log("WARNING: в строке отсутствует символ /: {0}", docPartId);
                        continue;
                    }

                    var symbolId = docPartId.Substring(0, slashIndex);
                    var xmlPath = docPartId.Substring(slashIndex);

                    CodeXmlDoc doc;
                    if(!_newXmlDocDict.TryGetValue(symbolId, out doc))
                        _newXmlDocDict.Add(symbolId, doc = new CodeXmlDoc(symbolId));

                    doc.AddDocPart(xmlPath, arr[1].Trim());
                }

                if(_newXmlDocDict.Count > 0)
                    _csvDocFile = filePath;

                return Task.FromResult(0);
            }, "Загрузка CSV файла со структурой XML документации");

            //var paths = string.Join("\n", _newXmlDocDict.Keys.Select(k => k.Substring(k.IndexOf('/'))).Distinct());
            //File.WriteAllText("paths.txt", paths);
            //MessageBox.Show(this, "paths:\n" + , "paths");
        }

        async void SelectSolution_Click(object sender, RoutedEventArgs e) {
            var filePath = SelectCsvFile(_tbSlnText.Text, "Выбор файла решения Visual Studio", "Visual Studio solutions (*.sln)|*.sln");
            if(string.IsNullOrEmpty(filePath))
                return;

            if(_solution != null) {
                _solution.Dispose();
                _solution = null;
            }

            _solutionFile = null;
            _analysisDone = false;
            _rewriteDone = false;

            _tbSlnText.Text = filePath;
            var fileName = Path.GetFileName(filePath);

            await BlockInputAction(async () => {
                Log("Загрузка решения {0}...", filePath);

                _solution = new VSSolution(filePath);

                await _solution.LoadSolution();

                Log("Загружено решение {0}. {1} проектов, {2} файлов.", fileName, _solution.NumProjects, _solution.NumDocuments);

                var progress = new Progress<string>(msg => {Dispatcher.MyGuiAsync(() => { _busyIndicator.BusyContent = msg; });});

                await _solution.CreateSemanticModel(progress);

                await _solution.CreateSymbolsTable(progress);

                // build and error check doesn't work for WPF projects
                // if(CheckSolutionErrors())
                //     return;

                if(_solution.NumProjects > 0 && _solution.NumDocuments > 0)
                    _solutionFile = fileName;

            }, "Загрузка " + fileName + "...");
        }

        async void _btnAnalysis_OnClick(object sender, RoutedEventArgs args) {
            if(!DataIsLoaded)
                return;

            await BlockInputAction(() => {
                try {
                    _analysisDone = false;
                    _oldXmlDocDict.Clear();

                    Log("---------------- Анализ входных данных:\n{0}\n{1}\n{2}\n----------------", _resourcesFile, _csvDocFile, _solutionFile);

                    var allFound = true;
                    foreach(var kv in _newXmlDocDict) {
                        var symbolId = kv.Key;
                        var newXmlDoc = kv.Value;

                        if(!newXmlDoc.CheckResources(_resourcesDict))
                            continue;

                        var oldXmlDoc = _solution.GetXmlDocForSymbol(symbolId);

                        if(oldXmlDoc == null) {
                            allFound = false;
                            continue;
                        }

                        if(!CodeXmlDoc.IsDocStructureTheSame(oldXmlDoc, newXmlDoc)) {
                            Log("ERROR: Структура документации в существующем коде и во входном файле не совпадают для символа {0}", symbolId);
                            continue;
                        }

                        _oldXmlDocDict[symbolId] = oldXmlDoc;
                    }

                    Log("------------------------------------------------------------------------");

                    _analysisDone = _newXmlDocDict.Count > 0 && _oldXmlDocDict.Count > 0;

                    Log("Анализ завершен. " + (_analysisDone ? "Можно выполнять замену в коде." : "Нет ни одного символа для замены документации."));

                    var skipSymbols = _newXmlDocDict.Keys.Except(_oldXmlDocDict.Keys).ToArray();
                    if(skipSymbols.Length > 0)
                        Log("Следующие символы проигнорированы:\n{0}", string.Join("\n", skipSymbols));

                } catch(Exception e) {
                    ErrorMsg("Ошибка во время анализа: " + e);
                }

                return Task.FromResult(0);
            });
        }

        async void _btnReplace_OnClick(object sender, RoutedEventArgs e) {
            if(!DataIsLoaded || !_analysisDone)
                return;

            var question = string.Format("Внимание! Если продолжить, то начнется замена документации в исходном коде решения {0}. Крайне рекомендуется сначала выполнить коммит для всех несохраненных изменений в репозитории, при наличии таковых. В противном случае при возникновении ошибок будет очень сложно разделить нужные и ненужные изменения.\n\nНачать замену документации в коде?\n",
                                         Path.GetFileName(_solutionFile));

            if(MessageBox.Show(this, question, "Продолжить?", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            Log("Начало изменения кода решения {0}", Path.GetFileName(_solutionFile));

            await BlockInputAction(async () => {
                var progress = new Progress<string>(msg => {Dispatcher.MyGuiAsync(() => { _busyIndicator.BusyContent = msg; });});

                var numUpdatedFiles = await _solution.RewriteDocuments(progress, _oldXmlDocDict, _newXmlDocDict, _oldXmlDocDict.Keys.ToArray(), _resourcesDict);

                _rewriteDone = numUpdatedFiles > 0;
            }, "Замена xml документации...");
        }

        bool CheckSolutionErrors() {
            var errors = _solution.GetErrorMessages().ToArray();

            if(errors.Length > 0) {
                ErrorMsg("В некоторых проектах обнаружены ошибки. Необходимо их исправить до начала работы по изменению XML документации.");

                Log("Ошибки компиляции:");
                foreach(var t in errors) {
                    var proj = t.Item1;
                    var errList = t.Item2;

                    Log("----- {0} -----", proj.Name);

                    foreach(var err in errList)
                        Log("{0}", err);
                }

                return true;
            }

            return false;
        }

        void UpdateButtonsState() {
            _btnAnalysis.IsEnabled = DataIsLoaded && !_rewriteDone;
            _btnReplace.IsEnabled = DataIsLoaded && _analysisDone && !_rewriteDone;
        }

        #region helpers

        async Task BlockInputAction(Func<Task> action, string message = null) {
            _busyIndicator.BusyContent = message;
            _busyIndicator.IsBusy = true;
            try {
                await action();
            } catch(Exception e) {
                ErrorMsg("Ошибка во время выполнения действия: " + e);
            } finally {
                _busyIndicator.IsBusy = false;
                UpdateButtonsState();
            }
        }

        static string SelectCsvFile(string currentSelection, string title, string filter) {
            string dir;

            currentSelection = currentSelection != null ? currentSelection.Trim() : null;

            try {
                dir = !string.IsNullOrEmpty(currentSelection) ? Path.GetDirectoryName(currentSelection) : (LastSelectDirectory ?? Directory.GetCurrentDirectory());
            } catch {
                dir = string.Empty;
            }

            var dialog = new OpenFileDialog
            {
                Filter = filter, 
                Title = title,
                CheckFileExists = true, 
                Multiselect = false, 
                InitialDirectory = dir
            };

            if(dialog.ShowDialog() == true) {
                LastSelectDirectory = Path.GetDirectoryName(dialog.FileName);
                return dialog.FileName;
            }

            return null;
        }

        List<string[]> ReadCsvFile(string path, int expectedFieldsCount) {
            var result = new List<string[]>();

            var unexpectedNum = 0;

            try {
                Log("Загрузка файла {0}...", path);

                using(var reader = new CsvFileReader(path)) {
                    var cols = new List<string>();

                    while(reader.ReadRow(cols)) {
                        if(cols.Count == 0) continue;

                        if(cols.Count != expectedFieldsCount) {
                            ++unexpectedNum;
                            Log("WARNING: Неожиданное количество колонок - {0}. Строка проигнорирована:\n{1}", cols.Count, string.Join(";", cols));
                            continue;
                        }

                        result.Add(cols.ToArray());
                    }
                }
            } catch(Exception e) {
                Log("Ошибка загрузки CSV: {0}", e);
                return result;
            }

            if(unexpectedNum > 0) {
                Log("WARNING: {0} строк содержали неверное количество столбцов. Ожидалось {1}.", unexpectedNum, expectedFieldsCount);
            }

            Log("Загружено {0} строк.", result.Count);

            return result;
        }

        void ErrorMsg(string msg) {
            MessageBox.Show(this, msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void Log(string str, params object[] pars) {
            Dispatcher.MyGuiAsync(() => {
                var message = string.Format(str, pars);
                _tbLog.AppendText(string.Format("{0:HH:mm:ss}: {1}\n", DateTime.Now, message));
                _tbLog.ScrollToEnd();
            });
        }

        #endregion
    }

    class StringResource {
        readonly string _key, _eng, _rus;

        public string Key {get {return _key;}}
        public string Eng {get {return _eng;}}
        public string Rus {get {return _rus;}}

        public StringResource(string key, string eng, string rus) {
            _key = key;
            _eng = eng;
            _rus = rus;
        }
    }

    public static class Extensions {
        public static void MyGuiAsync(this Dispatcher dispatcher, Action action) {
            if(dispatcher.CheckAccess())
                action();
            else
                dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
        }

        public static bool IsSupportedDeclaration(this SyntaxKind kind) {
            switch(kind) {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    return true;
            }

            return false;
        }

        public static bool IsDocumentCommentTrivia(this SyntaxKind triviaKind) {
            switch(triviaKind) {
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                case SyntaxKind.EndOfDocumentationCommentToken:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    return true;
            }

            return false;
        }
    }
}
