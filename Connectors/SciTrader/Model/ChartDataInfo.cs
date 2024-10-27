using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SciTrader.Model
{
    public class ChartDataInfo
    {
        public string ItemCode { get; set; }
        public string ChartType { get; set; } // "TICK", "MIN", "DAY", "WEEK", "MONTH", "YEAR"
        public int Cycle { get; set; }

        public ChartDataInfo(string itemCode, string chartType, int cycle)
        {
            ItemCode = itemCode;
            ChartType = chartType;
            Cycle = cycle;
        }

        public string GetKey()
        {
            return $"{ItemCode}-{ChartType}-{Cycle}";
        }
    }
}
