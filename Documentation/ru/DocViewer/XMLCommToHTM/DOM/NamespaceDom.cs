using System.Xml.Linq;

namespace XMLCommToHTM.DOM
{
	using Wintellect.PowerCollections;

	public class NamespaceDom
	{
		public OrderedBag<TypeDom> Types = new OrderedBag<TypeDom>((t1, t2) => t1.SimpleName.CompareTo(t2.SimpleName));
		public XElement DocInfo;

		private readonly string _name;

		public NamespaceDom(string name)
		{
			_name = name;
		}

		public string Name
		{
			get { return _name; }
		}
	}
}
