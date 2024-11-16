using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SciTrader.Model
{
    public class YearMonth
    {
        public string ProductCode { get; set; }
        public string YearMonthCode { get; set; }
        private List<StockItem> ItemList { get; set; }

        public YearMonth()
        {
            ItemList = new List<StockItem>();
        }

        public List<StockItem> GetItemList()
        {
            return new List<StockItem>(ItemList);
        }

        public void AddItem(StockItem item)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.ItemCode))
            {
                // Add item only if it doesn't already exist in the list
                if (!ItemList.Any(i => i.ItemCode == item.ItemCode))
                {
                    ItemList.Add(item);
                }
            }
        }

        public StockItem GetLatestStockItem()
        {
            if (ItemList.Count == 0) return null;
            // Assuming latest means the most recently added item
            return ItemList.LastOrDefault();
        }
    }

}
