using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ecng.Collections;
using Ecng.Common;
using Ecng.Xaml;
using MoreLinq;

namespace CsLocTool {
	public class StringResource : ViewModelBase 
	{
		string _constantName, _engString, _rusString;
		string _strLowerCase;

		readonly ObservableCollection<SourceCodeLiteral> _literals = new ObservableCollection<SourceCodeLiteral>(); 
		public IEnumerable<SourceCodeLiteral> Literals {get {return _literals;}}

		public StringResource(bool isNew, IEnumerable<SourceCodeLiteral> literals = null)
		{
			IsNew = isNew;
			_constantName = _engString = _rusString = string.Empty;

			if(literals != null)
				_literals.AddRange(literals);
		}

		public string ConstantName
		{
			get { return _constantName; }
			set
			{
				IsModified |= SetField(ref _constantName, value, () => ConstantName);
				UpdateLowerCase();
			}
		}

		public string EngString
		{
			get { return _engString; }
			set
			{
				IsModified |= SetField(ref _engString, value, () => EngString);
				UpdateLowerCase();
			}
		}

		public string RusString
		{
			get { return _rusString; }
			set
			{
				IsModified |= SetField(ref _rusString, value, () => RusString);
				UpdateLowerCase();
			}
		}

		public bool IsNew {get; private set;}
		public bool IsModified {get; set;}

		public bool IsNewOrModified
		{
			get { return IsNew || IsModified || _literals.Count > 0; }
		}

		public bool ContainsText(string txtFilter)
		{
			if(txtFilter.IsEmpty())
				return true;

			return _strLowerCase.Contains(txtFilter.ToLower(MainWindow.RuCulture));
		}

		void UpdateLowerCase()
		{
			_strLowerCase = (_constantName + _engString + _rusString).ToLower(MainWindow.RuCulture);
		}

		public void AddLiterals(IEnumerable<SourceCodeLiteral> literals)
		{
			literals.ForEach(l => _literals.Add(l));
		}

		public void RemoveLiterals(IEnumerable<SourceCodeLiteral> literals)
		{
			literals.ForEach(l => _literals.Remove(l));
		}
	}
}
