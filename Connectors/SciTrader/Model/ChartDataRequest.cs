using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SciTrader.Model
{
    public class ChartDataRequest
    {
        public string Protocol { get; set; } = "overseas_chart_inquiry1";
        public string ItemCode { get; set; }
        public string ChartType { get; set; }
        public int Cycle { get; set; }
        public int Next { get; set; }
        public int Count { get; set; }

        private string PrevKey = " ";
        private int Continuous = 0;

        public ChartDataRequest(string itemCode, string chartType, int cycle, int next, int count)
        {
            ItemCode = itemCode;
            ChartType = chartType;
            Cycle = cycle;
            Next = next;
            Count = count;
        }

        public string ToJson()
        {
            var json = new JObject
            {
                { "protocol", Protocol },
                { "itemCode", ItemCode },
                { "chartType", ChartType },
                { "cycle", Cycle },
                { "next", Next },
                { "count", Count },
                { "prevKey", PrevKey },
                { "continuous", Continuous }
            };

            return json.ToString();
        }
    }

}
