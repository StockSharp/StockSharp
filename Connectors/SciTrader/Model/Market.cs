using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SciTrader.Model
{
    public class Market
    {
        private string _name;
        private string _marketCode;
        private List<Product> _categoryList;

        public Market()
        {
            _categoryList = new List<Product>();
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string MarketCode
        {
            get { return _marketCode; }
            set { _marketCode = value; }
        }

        public Product AddProduct(string code)
        {
            var product = new Product { ProductCode = code };
            _categoryList.Add(product);
            return product;
        }

        public Product FindProduct(string code)
        {
            return _categoryList.Find(p => p.ProductCode == code);
        }

        public Product FindAddProduct(string code)
        {
            var product = FindProduct(code);
            if (product == null)
            {
                product = AddProduct(code);
            }
            return product;
        }

        public List<Product> GetProductList()
        {
            return _categoryList;
        }
    }
}
