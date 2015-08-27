using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace CsDocReplaceTool {
    class VSSolution : IDisposable {
        readonly string _path;
        readonly Dictionary<string, ISymbol> _symbols = new Dictionary<string, ISymbol>(); 
        readonly Dictionary<string, SemanticModel> _semanticModelsDict = new Dictionary<string, SemanticModel>();
        readonly CancellationTokenSource _cts = new CancellationTokenSource();
        CancellationTokenSource _ctsSemanticModel;

        public int NumProjects {get; private set;}
        public int NumDocuments {get; private set;}

        Solution _solution;

        bool _semanticModelWasBuilt;

        public Solution Solution {get {return _solution;}}

        public VSSolution(string path) {
            _path = path;
        }

        public async Task LoadSolution() {
            var workspace = MSBuildWorkspace.Create();

            _solution = await workspace.OpenSolutionAsync(_path, _cts.Token);

            NumProjects = _solution.Projects.Count();
            NumDocuments = _solution.Projects.SelectMany(p => p.Documents).Count();
        }

        public async Task CreateSemanticModel(IProgress<string> progress) {
            var numProcessed = 0;
            var allDocuments = _solution.Projects.SelectMany(p => p.Documents).ToArray();

            _semanticModelsDict.Clear();
            _semanticModelWasBuilt = false;

            _ctsSemanticModel = new CancellationTokenSource();

            File.WriteAllLines("file.out", allDocuments.GroupBy(d => d.FilePath).Where(g => g.Count() > 1).Select(g => g.Key));

            foreach(var doc in allDocuments) {
                if(string.IsNullOrEmpty(doc.FilePath))
                    continue;

                if(_semanticModelsDict.ContainsKey(doc.FilePath))
                    continue;

                var model = await doc.GetSemanticModelAsync();

                _semanticModelsDict.Add(doc.FilePath, model);

                progress.Report(string.Format("Построение семантической модели. Обработано {0} из {1} файлов...", ++numProcessed, NumDocuments));
            }

            _semanticModelWasBuilt = true;
        }

        public async Task CreateSymbolsTable(IProgress<string> progress) {
            if(!_semanticModelWasBuilt)
                throw new InvalidOperationException("CreateSymbolsTable: semantic model was not yet built");

            _symbols.Clear();

            await Task.Run(() => {
                var counter = 0;

                var symbols = _solution.Projects.SelectMany(p => p.Documents).SelectMany(d => {
                    if(string.IsNullOrEmpty(d.FilePath))
                        return Enumerable.Empty<ISymbol>();

                    SemanticModel model;

                    if(!_semanticModelsDict.TryGetValue(d.FilePath, out model)) {
                        MainWindow.Instance.Log("WARNING: семантическая модель для файла не найдена: {0}", d.FilePath);
                        return Enumerable.Empty<ISymbol>();
                    }

                    var s = GetAllSymbols(model.Compilation.Assembly.GlobalNamespace);

                    progress.Report(string.Format("Сбор символов {0}/{1}", ++counter, NumDocuments));

                    return s;
                }).Distinct().ToArray();

                counter = 0;

                foreach(var sym in symbols) {
                    if(++counter % 1000 == 0)
                        progress.Report(string.Format("Генерация ключей для символов {0}/{1}", counter, symbols.Length));

                    var id = sym.GetDocumentationCommentId();
                    if(id == null) continue;

                    _symbols[id] = sym;
                }

                //var docs = _symbols.Values.Select(s => s.GetDocumentationCommentXml()).ToArray();
                //var sss = symbols.Where(s => s.Name == "MyInterfaceEvent").ToArray();
            });
        }

        public async Task<int> RewriteDocuments(IProgress<string> progress, Dictionary<string, CodeXmlDoc> oldXmlDocDict, Dictionary<string, CodeXmlDoc> newXmlDocDict, string[] idsToRewrite, Dictionary<string, StringResource> resourcesDict) {
            return await Task.Run(() => {
                var numUpdatedFiles = 0;

                try {
                    progress.Report("Разбиение списка символов по файлам...");

                    var symbolsByFile = new Dictionary<string, List<Tuple<string, SyntaxNode>>>(); // filename, list<id, syntaxnode>
                    foreach(var id in idsToRewrite) {
                        var symbol = _symbols[id];

                        var references = symbol.DeclaringSyntaxReferences;

                        if(references == null || references.Length == 0) {
                            MainWindow.Instance.Log("ERROR: не удалось определить местоположение символа {0}", id);
                            continue;
                        }

                        foreach(var r in symbol.DeclaringSyntaxReferences) {
                            if(!r.SyntaxTree.HasCompilationUnitRoot)
                                MainWindow.Instance.Log("WARNING: не удалось определить единицу компиляции для {0}", id);

                            if(string.IsNullOrEmpty(r.SyntaxTree.FilePath)) {
                                MainWindow.Instance.Log("ERROR: не удалось определить имя файла для {0}", id);
                                continue;
                            }

                            List<Tuple<string, SyntaxNode>> list;
                            if(!symbolsByFile.TryGetValue(r.SyntaxTree.FilePath, out list))
                                symbolsByFile.Add(r.SyntaxTree.FilePath, list = new List<Tuple<string, SyntaxNode>>());

                            var node = r.GetSyntax();
                            var origNode = node;
                            var found = true;

                            while(!node.Kind().IsSupportedDeclaration()) {
                                if(node.Parent == null) {
                                    MainWindow.Instance.Log("Не найден поддерживаемый тип объявления символа:\nnode.Kind={0}, file={1}, id={2}", origNode.Kind(), r.SyntaxTree.FilePath, id);
                                    found = false;
                                    break;
                                }
                                node = node.Parent;
                            }

                            if(!found)
                                continue;

                            list.Add(Tuple.Create(id, node));
                        }
                    }

                    MainWindow.Instance.Log("Замена будет произведена в {0} файлах.", symbolsByFile.Count);

                    var documentsByPath = new Dictionary<string, Document>();
                    foreach(var doc in _solution.Projects.SelectMany(p => p.Documents).Where(doc => symbolsByFile.ContainsKey(doc.FilePath))) {
                        if(documentsByPath.ContainsKey(doc.FilePath))
                            MainWindow.Instance.Log("WARNING: на файл {0} ссылаются разные проекты", doc.FilePath);

                        documentsByPath[doc.FilePath] = doc;
                    }

                    var notFoundKeys = symbolsByFile.Keys.Where(k => !documentsByPath.ContainsKey(k)).ToArray();
                    if(notFoundKeys.Length > 0)
                        MainWindow.Instance.Log("ERROR: объект документа не найден для файлов:\n{0}", string.Join("\n", notFoundKeys));

                    foreach(var kv in symbolsByFile) {
                        Document document;
                        var filePath = kv.Key;
                        var symbols = kv.Value;

                        if(!documentsByPath.TryGetValue(filePath, out document))
                            continue;

                        progress.Report("Обработка файла " + filePath);

                        var idByNode = symbols.ToDictionary(s => s.Item2, s => s.Item1);
                        var root = (CompilationUnitSyntax)document.GetSyntaxRootAsync().Result;
                        var numReplacements = 0;

                        root = root.ReplaceNodes(idByNode.Keys, (origNode, rewrittenNode) => {
                            var id = idByNode[origNode];
                            var trivia = rewrittenNode.GetLeadingTrivia();

                            if(trivia.Count == 0) {
                                MainWindow.Instance.Log("ERROR: не найден xml комментарий для символа {0}", id);
                                return rewrittenNode;
                            }

                            var indentTrivia = trivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
                            if(indentTrivia == default(SyntaxTrivia)) {
                                MainWindow.Instance.Log("WARNING: не удалось определить отступ комментария для символа {0}", id);
                                indentTrivia = SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "\t");
                            }

                            var newTriviaList = new List<SyntaxTrivia>();
                            for(var i = 0; i < trivia.Count - 1; ++i) {
                                if(trivia[i].Kind().IsDocumentCommentTrivia())
                                    continue;

                                if(trivia[i].IsKind(SyntaxKind.WhitespaceTrivia) && trivia[i + 1].Kind().IsDocumentCommentTrivia())
                                    continue;

                                newTriviaList.Add(trivia[i]);
                            }

                            var endLineTrivia = SyntaxFactory.EndOfLine("\r\n");
                            if(newTriviaList.Count > 0 && !newTriviaList[newTriviaList.Count - 1].IsKind(SyntaxKind.EndOfLineTrivia))
                                newTriviaList.Add(endLineTrivia);

                            var newDoc = newXmlDocDict[id];
                            var newLines = newDoc.GetDocumentComments(resourcesDict);

                            foreach(var commentLine in newLines) {
                                newTriviaList.Add(indentTrivia);
                                newTriviaList.Add(SyntaxFactory.Comment(commentLine));
                                newTriviaList.Add(endLineTrivia);
                            }

                            newTriviaList.Add(indentTrivia);

                            var result = rewrittenNode.WithLeadingTrivia(newTriviaList);
                            ++numReplacements;

                            return result;
                        });

                        if(numReplacements == 0) {
                            MainWindow.Instance.Log("WARNING: Ни одной замены в файле {0}", document.FilePath);
                        } else {
                            File.WriteAllText(document.FilePath, root.ToFullString());
                            ++numUpdatedFiles;
                            MainWindow.Instance.Log("Файл обновлен ({0} замен): {1}", numReplacements, document.FilePath);
                        }
                    }
                } catch(Exception e) {
                    MainWindow.Instance.Log("ERROR: Исключение во время замены в файлах. При наличии несохраненных изменений необходимо откатить их и начать заново.\n{0}", e);
                    throw;
                } finally {
                    MainWindow.Instance.Log("Завершено. Обновлено {0} файлов.", numUpdatedFiles);
                }

                return numUpdatedFiles;
            });
        }

        IEnumerable<ISymbol> GetAllSymbols(INamespaceOrTypeSymbol root) {
            foreach(var m in root.GetMembers()) {
                yield return m;

                var r2 = m as INamespaceOrTypeSymbol;
                if(r2 != null)
                    foreach(var m2 in GetAllSymbols(r2))
                        yield return m2;
            }
        }

        public CodeXmlDoc GetXmlDocForSymbol(string symbolId) {
            ISymbol symbol;

            if(!_symbols.TryGetValue(symbolId, out symbol)) {
                MainWindow.Instance.Log("ERROR: в загруженном решении символ не обнаружен: {0}", symbolId);
                return null;
            }

            var xmlStr = symbol.GetDocumentationCommentXml();
            if(string.IsNullOrEmpty(xmlStr)) {
                var err = string.Format("ERROR: существующая xml документация для символа не найдена: {0}", symbolId);
                MainWindow.Instance.Log(err);
                return null;
            }

            var doc = new CodeXmlDoc(symbolId);

            try {
                doc.ParseFromXml(xmlStr);
            } catch(Exception e) {
                MainWindow.Instance.Log("ERROR: Не удалось распарсить xml документацию для символа {0}:\n{1}", symbolId, e);
                return null;
            }

            return doc;
        }

        bool SymbolIsSupported(ISymbol symbol) {
            switch(symbol.Kind) {
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.NamedType:
                case SymbolKind.Property:
                    return true;
                case SymbolKind.Method:
                    var ms = (IMethodSymbol)symbol;
                    switch(ms.MethodKind) {
                        case MethodKind.Constructor:
                        case MethodKind.Destructor:
                        case MethodKind.Conversion:
                        case MethodKind.ExplicitInterfaceImplementation:
                        case MethodKind.Ordinary:
                        case MethodKind.StaticConstructor:
                        case MethodKind.UserDefinedOperator:
                            return true;
                    }
                    break;
            }

            return false;
        }

        public IEnumerable<Tuple<Project, Diagnostic[]>> GetErrorMessages() {
            if(!_semanticModelWasBuilt)
                throw new InvalidOperationException("GetErrorMessages: semantic model was not yet built");

            return _solution.Projects.Select(p => 
                        Tuple.Create(p, p.GetCompilationAsync().Result.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray()))
                        .Where(t => t.Item2.Length > 0);
        } 

        public void Dispose() {
            _cts.Cancel();
            if(_ctsSemanticModel != null)
                _ctsSemanticModel.Cancel();
        }
    }
}
