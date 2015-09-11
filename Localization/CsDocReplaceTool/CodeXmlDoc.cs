using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace CsDocReplaceTool {
	using System.IO;
	using System.Text.RegularExpressions;
	using System.Xml.Linq;

	using Ecng.Common;

	class CodeXmlDoc {
        const string NameSummary    = "summary";
        const string NameTypeParam  = "typeparam";
        const string NameParam      = "param";
        const string NameReturns    = "returns";
        const string NameRemarks    = "remarks";

        readonly string _symbolId;
        static readonly string[] _supportedXmlElements = {
            NameSummary,
            NameTypeParam,
            NameParam,
            NameReturns,
            NameRemarks
        };

		static HashSet<string> _missedTranslations = new HashSet<string>();
        readonly Dictionary<string, string> _docParts = new Dictionary<string, string>();

        public string SymbolId {get {return _symbolId;}}

        public CodeXmlDoc(string symbolId) {
            _symbolId = symbolId;
        }

        public void AddDocPart(string xmlPath, string resourceId) {
            if(_docParts.ContainsKey(xmlPath)) {
                MainWindow.Instance.Log("ERROR: повторяющийся путь для документации: {0} (id = {1})", xmlPath, SymbolId);
                return;
            }

            _docParts.Add(xmlPath, resourceId);
        }

        public bool CheckResources(Dictionary<string, StringResource> resourcesDict) {
            var result = true;
            foreach(var kv in _docParts) {
                if(!resourcesDict.ContainsKey(kv.Value)) {
                    result = false;
                    MainWindow.Instance.Log("Не найден ресурс для кода {0} (используется символом {1})", kv.Value, SymbolId);
                }
            }

            return result;
        }

        public void ParseFromXml(string xmlStr) {
            _docParts.Clear();

            var document = new XmlDocument();
            document.LoadXml(xmlStr);

            var allElements = document.SelectNodes("member/*");

            if(allElements == null)
                throw new InvalidOperationException("Ошибка получения списка XML элементов для символа");

            var unsupported = allElements.Cast<XmlNode>().Where(n => !_supportedXmlElements.Contains(n.Name)).Select(n => n.Name).ToArray();
            if(unsupported.Length > 0)
                throw new InvalidOperationException("Неизвестные XML элементы в документации символа: " + string.Join(", ", unsupported));

            foreach(var node in allElements.Cast<XmlNode>()) {
                string key;
                if(node.Name == NameTypeParam || node.Name == NameParam) {
                    if(node.Attributes == null)
                        throw new InvalidOperationException("Отсутствуют атрибуты элемента " + node.Name);

                    key = string.Format("/{0}/{1}", node.Name, node.Attributes["name"].Value);
                } else {
                    key = "/" + node.Name;
                }

                _docParts[key] = null; // value is not used (if we parse XML - this is old comments)
            }
        }

		public IEnumerable<string> GetDocumentComments(Dictionary<string, StringResource> resourcesDict, Dictionary<string, XElement[]> docElements)
		{
            var result = new List<string>();
            var sortedKeys = GetSortedValuesWithType();

            foreach(var t in sortedKeys) {
                var elementName = t.Item1;
                var keykey = t.Item2;

	            var r = resourcesDict[_docParts[keykey]];

				if (r.Rus == r.Eng && _missedTranslations.Add(r.Rus))
	            {
					File.AppendAllLines("missed_translation.txt", new[] { r.Rus });
					MainWindow.Instance.Log("WARNING: '{0}' не имеет перевода", r.Rus);
	            }

                var newText = r.Eng;

	            if (!newText.EndsWith("."))
		            newText += ".";

				newText = newText.Put(docElements[_symbolId + keykey].Select(x =>
				{
					if (x.Name != "see")
						return x;

					var attr = x.Attribute("cref");

					if (attr == null)
						return x;

					var cref = attr.Value;

					var ind = cref.LastIndexOf('.');
					if (ind != -1)
					{
						if (!cref.StartsWith("T:"))
						{
							var ind2 = cref.LastIndexOf('.', ind - 1);

							if (ind2 != -1)
								ind = ind2;
						}

						cref = cref.Remove(0, ind + 1);
					}

					var match = Regex.Match(cref, @"`(?<count>\d+)$");
					if (match.Success)
					{
						var group = match.Groups["count"];
						var paramCount = group.Value.To<int>();
						cref = cref.Substring(0, group.Index - 1) + "{";

						if (paramCount == 1)
							cref += "T";
						else
							cref += Enumerable.Repeat("T", paramCount).Select((v, i) => v + (i + 1)).Join(",");
						
						cref += "}";
					}

					return (object)"<see cref=\"{0}\"/>".Put(cref);
				}).ToArray());

                switch(elementName) {
                    case NameSummary:
                    case NameRemarks:
                        result.Add(string.Format("/// <{0}>", elementName));
                        result.AddRange(newText.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries).Select(l => "/// " + l));
                        result.Add(string.Format("/// </{0}>", elementName));
                        break;
                    case NameReturns:
                        newText = newText.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                        result.Add(string.Format("/// <returns>{0}</returns>", newText));
                        break;
                    case NameTypeParam:
                    case NameParam:
                        var paramName = keykey.Substring(keykey.LastIndexOf('/') + 1);
                        newText = newText.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                        result.Add(string.Format("/// <{0} name=\"{1}\">{2}</{0}>", elementName, paramName, newText));
                        break;
                }
            }

            return result;
        }

        IEnumerable<Tuple<string, string>> GetSortedValuesWithType() {
            var result = new List<Tuple<string, string>>();

            foreach(var name in _supportedXmlElements) {
                result.AddRange(_docParts.Keys.Where(k => k.StartsWith("/" + name)).Select(k => Tuple.Create(name, k)));
            }

            return result;
        }

        public static bool IsDocStructureTheSame(CodeXmlDoc doc1, CodeXmlDoc doc2) {
            return doc1._docParts.Count == doc2._docParts.Count &&
                   doc1._docParts.Keys.All(k => doc2._docParts.ContainsKey(k));
        }
    }
}
