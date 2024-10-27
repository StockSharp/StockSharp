using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SciTrader.Model
{
    public class Product
    {
        public string ProductCode { get; set; }
        public string NameKr { get; set; }
        public string Name { get; set; }
        public string Exchange { get; set; }
        public string ExchangeCode { get; set; }
        public string ExchangeIndex { get; set; }
        public string MarketName { get; set; }
        public string MarketCode { get; set; }
        public int Decimal { get; set; }

        private List<StockItem> _itemList;
        private Dictionary<string, YearMonth> _yearMonthMap;
        private Dictionary<string, string> _domesticYearTable;
        private Dictionary<string, string> _domesticMonthTable;
        private Dictionary<string, string> _abroadMonthTable;

        public Product()
        {
            _itemList = new List<StockItem>();
            _yearMonthMap = new Dictionary<string, YearMonth>();
            _domesticYearTable = new Dictionary<string, string>();
            _domesticMonthTable = new Dictionary<string, string>();
            _abroadMonthTable = new Dictionary<string, string>();
            InitializeTables();
        }

        private void InitializeTables()
        {
            _domesticYearTable["A"] = "2006";
            _domesticYearTable["B"] = "2007";
            _domesticYearTable["C"] = "2008";
            _domesticYearTable["D"] = "2009";
            _domesticYearTable["E"] = "2010";
            _domesticYearTable["F"] = "2011";
            _domesticYearTable["G"] = "2012";
            _domesticYearTable["H"] = "2013";
            _domesticYearTable["J"] = "2014";
            _domesticYearTable["K"] = "2015";
            _domesticYearTable["L"] = "2016";
            _domesticYearTable["M"] = "2017";
            _domesticYearTable["N"] = "2018";
            _domesticYearTable["P"] = "2019";
            _domesticYearTable["Q"] = "2020";
            _domesticYearTable["R"] = "2021";
            _domesticYearTable["S"] = "2022";
            _domesticYearTable["T"] = "2023";
            _domesticYearTable["V"] = "2024";
            _domesticYearTable["W"] = "2025";

            _domesticMonthTable["1"] = "01";
            _domesticMonthTable["2"] = "02";
            _domesticMonthTable["3"] = "03";
            _domesticMonthTable["4"] = "04";
            _domesticMonthTable["5"] = "05";
            _domesticMonthTable["6"] = "06";
            _domesticMonthTable["7"] = "07";
            _domesticMonthTable["8"] = "08";
            _domesticMonthTable["9"] = "09";
            _domesticMonthTable["A"] = "10";
            _domesticMonthTable["B"] = "11";
            _domesticMonthTable["C"] = "12";

            _abroadMonthTable["F"] = "01";
            _abroadMonthTable["G"] = "02";
            _abroadMonthTable["H"] = "03";
            _abroadMonthTable["J"] = "04";
            _abroadMonthTable["K"] = "05";
            _abroadMonthTable["M"] = "06";
            _abroadMonthTable["N"] = "07";
            _abroadMonthTable["Q"] = "08";
            _abroadMonthTable["U"] = "09";
            _abroadMonthTable["V"] = "10";
            _abroadMonthTable["X"] = "11";
            _abroadMonthTable["Z"] = "12";
        }

        public StockItem AddItem(StockItem item)
        {
            _itemList.Add(item);
            return item;
        }

        public List<StockItem> GetItemList()
        {
            return _itemList;
        }

        public StockItem GetRecentMonthItem()
        {
            return _itemList.FirstOrDefault();
        }

        public StockItem GetNextMonthItem()
        {
            return _itemList.ElementAtOrDefault(1);
        }

        public YearMonth GetRecentYearMonth()
        {
            return _yearMonthMap.Values.FirstOrDefault();
        }

        public YearMonth GetNextYearMonth()
        {
            return _yearMonthMap.Values.Skip(1).FirstOrDefault();
        }

        public YearMonth GetYearMonth(string yearMonth)
        {
            _yearMonthMap.TryGetValue(yearMonth, out var yearMonthObj);
            return yearMonthObj;
        }

        public List<StockItem> GetRecentMonthItemOnOption()
        {
            var ym = GetRecentYearMonth();
            return ym?.GetItemList() ?? new List<StockItem>();
        }

        public void AddToYearMonth(int market, string itemCode, StockItem item)
        {
            if (item == null)
                return;

            string localDateTime = DateTime.Now.ToString("yyyyMMdd");
            string localYearMonth = localDateTime.Substring(0, 6);

            if (market == 0) // Domestic product
            {
                string productCode = itemCode.Substring(0, 3);
                string year = itemCode.Substring(3, 1);
                year = _domesticYearTable[year];
                string month = itemCode.Substring(4, 1);
                month = _domesticMonthTable[month];
                string yearMonthTemp = year + month;
                string yearMonth = year + "-" + month;
                if (string.Compare(yearMonthTemp, localYearMonth) < 0)
                    return;

                if (!_yearMonthMap.TryGetValue(yearMonth, out var ym))
                {
                    ym = new YearMonth { ProductCode = productCode, YearMonthCode = yearMonth };
                    _yearMonthMap[yearMonth] = ym;
                }
                else
                {
                    return;
                }
                ym.AddItem(item);
            }
            else // Abroad product
            {
                string productCode = itemCode.Substring(0, 2);
                string year = "20" + itemCode.Substring(itemCode.Length - 2, 2);
                string month = itemCode.Substring(itemCode.Length - 3, 1);
                month = _abroadMonthTable[month];
                string yearMonth = year + "-" + month;
                string yearMonthTemp = year + month;
                if (string.Compare(yearMonthTemp, localYearMonth) < 0)
                    return;

                if (!_yearMonthMap.TryGetValue(yearMonth, out var ym))
                {
                    ym = new YearMonth { ProductCode = productCode, YearMonthCode = yearMonth };
                    _yearMonthMap[yearMonth] = ym;
                }
                else
                    return;
                ym.AddItem(item);
            }
        }

        public void AddToYearMonth(string itemCode, string name, StockItem item)
        {
            if (item == null)
                return;

            if (char.IsDigit(itemCode[2])) // Domestic product
            {
                string productCode = itemCode.Substring(0, 3);
                string yearMonthWeek = name.Substring(15, 6);

                string year = "20" + yearMonthWeek.Substring(0, 2);
                string month = yearMonthWeek.Substring(2, 2);
                string week = yearMonthWeek.Substring(4, 2);

                string localDateTime = DateTime.Now.ToString("yyyyMMdd");
                string localYearMonth = localDateTime.Substring(0, 6);
                string localMonth = localDateTime.Substring(4, 2);
                string weekInfo = yearMonthWeek.Substring(5, 1);
                string yearMonthTemp = year + month;
                string yearMonth = year + "-" + month + "-";

                if (string.Compare(yearMonthTemp, localYearMonth) < 0)
                    return;

                int[] dateTimeVec = { DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day };

                // Determine the day of the week for the first day of the month
                int dayWeek = Dow(dateTimeVec[0], dateTimeVec[1], 1);
                int wholeDays = dayWeek + dateTimeVec[2];
                wholeDays += 2; // Consider Thursday as the expiration day

                int q = wholeDays / 7;
                if (dayWeek == 6)
                {
                    q--;
                }

                int rem = wholeDays % 7;
                if (rem > 0)
                    q++;

                if (q > 5)
                    q = 5;

                if (int.Parse(month) <= int.Parse(localMonth) && int.Parse(weekInfo) < q)
                    return;

                yearMonth += week;

                if (!_yearMonthMap.TryGetValue(yearMonth, out var ym))
                {
                    ym = new YearMonth
                    {
                        ProductCode = productCode,
                        YearMonthCode = yearMonth
                    };
                    _yearMonthMap[yearMonth] = ym;
                }
                else
                    return;
                ym.AddItem(item);
            }
        }

        public int Dow(int y, int m, int d)
        {
            int[] t = { 0, 3, 2, 5, 0, 3, 5, 1, 4, 6, 2, 4 };
            if (m < 3)
            {
                y -= 1;
            }
            return (y + y / 4 - y / 100 + y / 400 + t[m - 1] + d) % 7;
        }
    }
}
