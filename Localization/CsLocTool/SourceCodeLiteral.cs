using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ecng.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CsLocTool {
	public class SourceCodeLiteral 
	{
		static long _lastId;

		readonly long _codeOrder;
		string _strLowerCase;
		string _linesTextLowerCase;

		public SourceCodeLiteral(Document document, LiteralExpressionSyntax expression)
		{
			_codeOrder = Interlocked.Increment(ref _lastId);

			Document = document;
			Expression = expression;
			_strLowerCase = expression.Token.Text.ToLower(MainWindow.RuCulture);

			LiteralId = MainWindow.GetStringHash(FilePath+Expression.Span+OriginalText);

			var attr = FindParentNode<AttributeSyntax>(expression);

			if (attr != null)
			{
				IsPartOfAttributeDeclaration = true;
				AttributeNameSyntax = attr.Name;
				AttributeName = AttributeNameSyntax.ToString();
			}

			if (!IsPartOfAttributeDeclaration)
			{
				IsSwitchCase = FindParentNode<CaseSwitchLabelSyntax>(expression) != null;

				if (!IsSwitchCase)
				{
					var fld = FindParentNode<FieldDeclarationSyntax>(expression);
					if (fld != null)
						IsConstString = fld.Modifiers.Any(m => m.CSharpKind() == SyntaxKind.ConstKeyword);
				}
			}
		}

		public void InitCodeLine(TextLineCollection lines)
		{
			var lineSpan = Expression.SyntaxTree.GetLineSpan(Expression.Span);

			var selectedLines = new List<string>();

			for(var i=lineSpan.StartLinePosition.Line; i<=lineSpan.EndLinePosition.Line; ++i)
				selectedLines.Add(lines[i].ToString());

			_linesTextLowerCase = selectedLines.Join("\n").ToLower(MainWindow.RuCulture);
		}

		public LiteralExpressionSyntax Expression {get; private set;}
		public long CodeOrder {get {return _codeOrder;}}
		public string OriginalText {get {return Expression.Token.Text;}}
		public string StringValue {get {return Expression.Token.ValueText;}}

		public Document Document {get; private set;}
		public string ProjectName {get {return Document.Project.Name;}}
		public string FilePath {get {return Document.FilePath;}}
		public bool IsPartOfAttributeDeclaration {get; private set;}
		public bool IsSwitchCase {get; private set;}
		public bool IsConstString {get; private set;}
		public string AttributeName {get; private set;}

		public NameSyntax AttributeNameSyntax {get; private set;}

		public string LiteralId {get; private set;}

		public bool ContainsText(string txtFilter)
		{
			if(txtFilter.IsEmpty())
				return true;

			return _strLowerCase.Contains(txtFilter.ToLower(MainWindow.RuCulture));
		}

		public bool CodeLineContainsText(string txtLineFilter)
		{
			if(txtLineFilter.IsEmpty())
				return true;

			return _linesTextLowerCase.Contains(txtLineFilter.ToLower(MainWindow.RuCulture));
		}

		public bool MatchFile(string fileFilter)
		{
			if(fileFilter.IsEmpty())
				return true;

			return FilePath.ToLower(MainWindow.RuCulture).Contains(fileFilter);
		}

		public static T FindParentNode<T>(SyntaxNode node) where T:SyntaxNode
		{
			var n = node.Parent;

			while(true)
				if(n == null || n is T)
					return n as T;
				else
					n = n.Parent;
		}

		public static T FindChildNode<T>(SyntaxNode node, int maxDepth = int.MaxValue, int depth = 0) where T:SyntaxNode
		{
			if(depth > maxDepth)
				return null;

			var children = node.ChildNodes();
			var result = children.FirstOrDefault(c => c is T) as T;

			if(result != null)
				return result;

			if(depth >= maxDepth)
				return null;

			foreach (var c in children)
			{
				result = FindChildNode<T>(c, maxDepth, depth+1);
				if(result != null)
					return result;
			}

			return null;
		}
	}
}
