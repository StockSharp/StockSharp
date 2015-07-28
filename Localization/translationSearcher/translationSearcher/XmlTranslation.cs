using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Web;

///	<summary/>
public class XmlTranslation
{
	/// <summary>
	/// Для каждого из описаний в <paramref name="xmlFile"/> найти перевод в файле <paramref name="textCsvFile"/>. 
	/// Если найден - сложить идентификатор описания и ключ перевода (колонка 0) в файл <paramref name="resultIdCsvFile"/>,
	/// иначе - дополнить файл <paramref name="textCsvFile"/> строкой со сгенерированным идентификатором перевода и текстом описания
	/// продублированным для обоих языков. Предполагается, что язык описаний в <paramref name="xmlFile"/> соответствует колонке с 
	/// индексом 2 в файле <paramref name="textCsvFile"/>
	/// </summary>
	/// <param name="xmlFile">путь к существующему файлу, который необходимо конвертировать</param>
	/// <param name="textCsvFile">путь к файлу https://github.com/StockSharp/StockSharp/blob/master/Localization/text.csv на диске</param>
	/// <param name="resultIdCsvFile">путь, где должен быть создан (или перезаписан с нуля) файл</param>
	public void SearchTranslationsForXml(string xmlFile, string textCsvFile, string resultIdCsvFile)
	{
		int newKeyCount = 0;

		var newKeyIndex = parseTextCsv(textCsvFile, m_TranslationKeys, 2);	//	предполагаем, что исходный файл на русском, т.е. колонка 2 в text.csv
		endOfLineCorrection(textCsvFile);	//	исходный csv не заканчивался на \n
		var outStreamTextCsv = new StreamWriter(textCsvFile, true);

		var outStream = new StreamWriter(resultIdCsvFile);

		var root = XDocument.Load(xmlFile);
		var members = root.Elements("doc").Elements("members").Elements("member");

		var newTranslations = new HashSet<string>();
		foreach (var member in members)
		{
			var name = member.Attribute("name");
			foreach (var tag in member.Elements())
			{
				XElement []tagChildren;
				var content = parseContent(tag, out tagChildren);
				if (!m_TranslationKeys.ContainsKey(content))
				{
					if (newTranslations.Contains(content))
						continue;
					newTranslations.Add(content);

					if (content.Contains(";"))
						content = "\"" + content + "\"";

					var newKey = prefix + newKeyIndex++.ToString();
					outStreamTextCsv.WriteLine(newKey + ";" + content + ";" + content);

					newKeyCount++;
				}
				else
					outStream.WriteLine(getPath(tag) + ";" + m_TranslationKeys[content].Item1);
			}
		}

		outStream.Close();
		outStreamTextCsv.Close();

		if (newKeyCount != 0)
			Console.WriteLine("не хватает " + newKeyCount.ToString() + " переводов");
		else
			Console.WriteLine("все переводы найдены");
	}

	/// <summary>
	/// Перевести описания в <paramref name="xmlFile"/> на язык, соответствующий колонке <paramref name="language"/> в файле 
	/// <paramref name="textCsvFile"/>, сопоставив идентификаторы описаний ключам перевода на основе файла 
	/// <paramref name="idCsvFile"/>
	/// </summary>
	/// <param name="xmlFile">путь к существующему файлу, который необходимо использовать как структуру для создания <paramref name="resultXmlFile"/></param>
	/// <param name="idCsvFile">путь к существующему файлу, созданный через <see cref="SearchTranslationsForXml"/></param> 
	/// <param name="textCsvFile"> путь к файлу https://github.com/StockSharp/StockSharp/blob/master/Localization/text.csv на диске</param>
	/// <param name="language">номер колонки с 1 означающий код языка в файле <paramref name="textCsvFile"/></param>
	/// <param name="resultXmlFile">путь, где должен быть создан (или перезаписан с нуля) файл</param>
	public void TranslateXml(string xmlFile, string idCsvFile, string textCsvFile, int language, string resultXmlFile)
	{
		parseTextCsv(textCsvFile, m_Translations, 0);	//	ключ словаря - первый столбец в файле, т.е. собственно ключ перевода
		m_TranslKeysForPath = parseIdCsv(idCsvFile);

		var root = XDocument.Load(xmlFile);
		var members = root.Elements("doc").Elements("members").Elements("member");

		foreach (var member in members)
		{
			var name = member.Attribute("name");
			foreach (var tag in member.Elements().ToArray())
				translate(tag, language, xmlFile, idCsvFile, textCsvFile);
		}

		root.Save(resultXmlFile);
	}

	private const string prefix = "DocStr";
	private Dictionary<string, Tuple<string, string>> m_Translations = new Dictionary<string, Tuple<string, string>>();	//	ключ словаря - ключ перевода из text.csv, Tuple - переводы
	private Dictionary<string, Tuple<string, string>> m_TranslationKeys = new Dictionary<string, Tuple<string, string>>();	//	ключ словаря - русский перевод (колонка 2 в файле text.csv), Tuple.Item1 - ключ перевода из text.csv
	private Dictionary<string, string> m_TranslKeysForPath;	//	ключ словаря - путь из файла idCsvFile, значение - соответствующий ключ перевода из text.csv

	private void translate(XElement tag, int language, string xmlFile, string idCsvFile, string textCsvFile)
	{
		var tagPath = getPath(tag);
		if (!m_TranslKeysForPath.ContainsKey(tagPath))
			throw new Exception("в файле " + idCsvFile + " не найден путь " + tagPath +
				" существующего элемента <member> в файле " + xmlFile + ", невозможно идентифицировать перевод");

		var translKey = m_TranslKeysForPath[tagPath];

		var original = tag.ToString();

		XElement[] tagChildren;
		var content = parseContent(tag, out tagChildren);

		if (!m_Translations.ContainsKey(translKey))
			throw new Exception("в файле " + textCsvFile + " не найден перевод по ключу " + translKey);

		if (language < 1 || language > 2)
			throw new Exception("индекс языка отличный от 1 или 2 не поддерживается");
		var translation = language == 1 ? m_Translations[translKey].Item1 : m_Translations[translKey].Item2;

		var encoded = HttpUtility.HtmlEncode(translation);	//	например, было содержимое "Свеча (X&amp;0)", превращавшееся в XElement.Value в "Свеча (X&0)", в этом месте преобразуем обратно

		var newContent = "";
		if (tagChildren.Count() != 0)
			try
			{
				newContent = string.Format(encoded, tagChildren);
			}
			catch (FormatException)
			{
				throw new Exception("Не удается сопоставить xml теги исходной строки формату перевода:\nисходная - " + original +
					"\nперевод - " + encoded);
			}
		else	//	встречаются странные строки без параметров, но с фигурными скобками
			newContent = encoded;

		var newText = "<" + tag.Name + ">" + newContent + "</" + tag.Name + ">";
		var newEl = XElement.Parse(newText);
		newEl.Add(tag.Attributes());

		tag.ReplaceWith(newEl);
	}

	private string cleanString(string s)
	{
		var ss = s.Split('\n');
		for (int i = 0; i < ss.Count(); i++ )
			ss[i] = ss[i].Trim();
		s = string.Join(" ", ss).Trim();
		return s.TrimEnd('.').Trim();
	}

	private string parseContent(XElement tag, out XElement []tagChildren)
	{
		tagChildren = tag.Elements().ToArray();

		int index = 0;
		while (tag.Elements().Count() != 0)
			tag.Elements().First().ReplaceWith("{" + index++ + "}");

		return cleanString(tag.Value);
	}

	private int parseTextCsv(string fName, Dictionary<string, Tuple<string, string>> res, uint dictKeyColIndex)
	{
		var file = new StreamReader(fName);
		string line;

		string[] ss = null;

		while ((line = file.ReadLine()) != null)
		{
			if (line.Contains(";\""))	//	например: Str3634Params;"New candle {0}: {6} {1};{2};{3};{4}; volume {5}";"Новая свеча {0}: {6} {1};{2};{3};{4}; объем {5}"
			{
				ss = line.Split(new string[]{";\""}, StringSplitOptions.None);
				if (ss.Length != 3)
					throw new Exception("text.csv содержит некорректную строку: \"" + line + "\"");
				ss[1] = ss[1].TrimEnd('\"');
				ss[2] = ss[2].TrimEnd('\"');
			}
			else
			{
				ss = line.Split(';');
				if (ss.Length != 3)
					throw new Exception("text.csv содержит некорректную строку: \"" + line + "\"");
			}

			ss[1] = cleanString(ss[1]);
			ss[2] = cleanString(ss[2]);

			if (dictKeyColIndex > 2)
				throw new Exception("Некорректный вызов parseTextCsv (баг в коде)");
			var indices = new List<uint> { 0, 1, 2 };
			indices.Remove(dictKeyColIndex);	//	ss для оставшихся двух индексов - в значение Dictionary

			res[ss[dictKeyColIndex]] = new Tuple<string, string>(ss[indices[0]], ss[indices[1]]);
		}
		file.Close();

		//	возвращаем индекс, с которого можно начинать нумерацию новых ключей <prefix>.. в text.csv
		if (ss != null && ss[0].StartsWith(prefix))
			return int.Parse(ss[0].Substring(prefix.Length)) + 1;
		else
			return 0;
	}

	private Dictionary<string, string> parseIdCsv(string fName)
	{
		var file = new StreamReader(fName);
		string line;

		string[] ss = null;
		var res = new Dictionary<string, string>();

		while ((line = file.ReadLine()) != null)
		{
			ss = line.Split(';');
			if (ss.Length != 2)
				throw new Exception(fName + " содержит некорректную строку: \"" + line + "\"");

			res[ss[0]] = ss[1];
		}
		file.Close();
		return res;
	}

	private void endOfLineCorrection(string fName)
	{
		if (!File.Exists(fName))
			return;
		var fi = new FileInfo(fName);
		if (fi.Length == 0)
			return;
			
		var f = File.Open(fName, FileMode.Open, FileAccess.ReadWrite);
		f.Seek(-1, SeekOrigin.End);
		char c = (char)f.ReadByte();
		if (c != '\n')
			f.WriteByte((byte)'\n');

		f.Close();
	}

	private string getPath(XElement tag)
	{
		var basic = tag.Parent.Attribute("name").Value + "/" + tag.Name;
		var nameAttr = tag.Attribute("name");
		if (nameAttr != null)
			return basic + "/" + nameAttr.Value;
		else
			return basic;
	}
};


