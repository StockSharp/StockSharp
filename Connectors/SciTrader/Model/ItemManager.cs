using NLog;
using SciChart.Charting3D.PointMarkers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using WebSocketSharp;

namespace SciTrader.Model
{
    public class ItemManager
    {
        // Singleton pattern to ensure a single instance
        private static readonly Lazy<ItemManager> _instance = new Lazy<ItemManager>(() => new ItemManager());
        public static ItemManager Instance => _instance.Value;

        // Dictionary to store StockItems
        private readonly Dictionary<string, StockItem> _items;

        // HashSet to store product codes
        private readonly HashSet<string> _FavoriteProductSet;

        private List<Market> _marketList;
        private Dictionary<string, string> _marketCodeToMarketNameTable;

        private readonly Dictionary<string, Product> _products;

        // Constructor
        public ItemManager()
        {
            _items = new Dictionary<string, StockItem>();
            _FavoriteProductSet = new HashSet<string>();
            _products = new Dictionary<string, Product>();
            InitProductSet(); // Initialize the product set
            _marketList = new List<Market>();
            _marketCodeToMarketNameTable = new Dictionary<string, string>();
            InitMarketTable();
        }

        public void InitMarketTable()
        {
            Market market;

            market = new Market
            {
                Name = "ETF",
                MarketCode = "009"
            };
            _marketList.Add(market);
            _marketCodeToMarketNameTable["009"] = "ETF";

            market = new Market
            {
                Name = "국내선물",
                MarketCode = "008"
            };
            _marketList.Add(market);
            _marketCodeToMarketNameTable["008"] = "국내선물";

            market = new Market
            {
                Name = "지수",
                MarketCode = "001"
            };
            _marketList.Add(market);
            _marketCodeToMarketNameTable["001"] = "지수";

            market = new Market
            {
                Name = "통화",
                MarketCode = "002"
            };
            _marketList.Add(market);
            _marketCodeToMarketNameTable["002"] = "통화";

            market = new Market
            {
                Name = "금리",
                MarketCode = "003"
            };
            _marketList.Add(market);
            _marketCodeToMarketNameTable["003"] = "금리";

            market = new Market
            {
                Name = "농축산",
                MarketCode = "004"
            };
            _marketList.Add(market);
            _marketCodeToMarketNameTable["004"] = "농축산";

            market = new Market
            {
                Name = "귀금속",
                MarketCode = "005"
            };
            _marketList.Add(market);
            _marketCodeToMarketNameTable["005"] = "귀금속";

            market = new Market
            {
                Name = "에너지",
                MarketCode = "006"
            };
            _marketList.Add(market);
            _marketCodeToMarketNameTable["006"] = "에너지";

            market = new Market
            {
                Name = "비철금속",
                MarketCode = "007"
            };
            _marketList.Add(market);
            _marketCodeToMarketNameTable["007"] = "비철금속";
        }

        // Method to save a StockItem
        public void SaveItem(StockItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemCode))
            {
                throw new ArgumentException("Item or ItemCode cannot be null or empty");
            }

            _items[item.ItemCode] = item;
        }

        // Method to read a StockItem by its ItemCode
        public StockItem ReadItem(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                throw new ArgumentException("ItemCode cannot be null or empty");
            }

            if (_items.TryGetValue(itemCode, out StockItem item))
            {
                return item;
            }

            return null;
        }

        // Method to delete a StockItem by its ItemCode
        public bool DeleteItem(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                throw new ArgumentException("ItemCode cannot be null or empty");
            }

            return _items.Remove(itemCode);
        }

        // Method to find a StockItem by its ItemCode
        public StockItem FindItem(string itemCode)
        {
            return ReadItem(itemCode);
        }

        // Method to get all StockItems
        public IEnumerable<StockItem> GetAllItems()
        {
            return _items.Values;
        }

        // Method to read StockItems from a file
        public void ReadABItemFile(string subDirectory, string fileName)
        {
            try
            {
                // Get the application path
                string appPath = AppDomain.CurrentDomain.BaseDirectory;

                // Combine the path to the subdirectory and file name
                string filePath = Path.Combine(appPath, subDirectory, fileName);

                // Specify the euc-kr encoding
                int euckrCodePage = 51949;  // euc-kr code page
                System.Text.Encoding eucKr = System.Text.Encoding.GetEncoding(euckrCodePage);

                // Read the file line by line
                using (StreamReader sr = new StreamReader(filePath, eucKr))
                {
                    string line;
                    int lineNum = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        byte[] lineBytes = eucKr.GetBytes(line);

                        string symbolCode = eucKr.GetString(lineBytes, 0, 32).Trim();
                        string exchangeName = eucKr.GetString(lineBytes, 32, 5).Trim();
                        string indexCode = eucKr.GetString(lineBytes, 37, 4).Trim();
                        string productCode = eucKr.GetString(lineBytes, 41, 5).Trim();
                        string exchNo = eucKr.GetString(lineBytes, 46, 5).Trim();
                        string pdesz = eucKr.GetString(lineBytes, 51, 5).Trim();
                        string rdesz = eucKr.GetString(lineBytes, 56, 5).Trim();
                        string ctrtSize = eucKr.GetString(lineBytes, 61, 20).Trim();
                        string tickSize = eucKr.GetString(lineBytes, 81, 20).Trim();
                        string tickValue = eucKr.GetString(lineBytes, 101, 20).Trim();
                        string mltiPler = eucKr.GetString(lineBytes, 121, 20).Trim();
                        string dispDigit = eucKr.GetString(lineBytes, 141, 10).Trim();
                        string symbolNameEn = eucKr.GetString(lineBytes, 151, 32).Trim();
                        string symbolNameKr = eucKr.GetString(lineBytes, 183, 32).Trim();
                        string nearSeq = eucKr.GetString(lineBytes, 215, 1).Trim();
                        string statTp = eucKr.GetString(lineBytes, 216, 1).Trim();
                        string lockDt = eucKr.GetString(lineBytes, 217, 8).Trim();
                        string tradFrDt = eucKr.GetString(lineBytes, 225, 8).Trim();
                        string lastDate = eucKr.GetString(lineBytes, 233, 8).Trim();
                        string exprDt = eucKr.GetString(lineBytes, 241, 8).Trim();
                        string remnCnt = eucKr.GetString(lineBytes, 249, 4).Trim();
                        string hogaMthd = eucKr.GetString(lineBytes, 253, 30).Trim();
                        string minMaxRt = eucKr.GetString(lineBytes, 283, 6).Trim();
                        string baseP = eucKr.GetString(lineBytes, 289, 20).Trim();
                        string maxP = eucKr.GetString(lineBytes, 309, 20).Trim();
                        string minP = eucKr.GetString(lineBytes, 329, 20).Trim();
                        string trstMgn = eucKr.GetString(lineBytes, 349, 20).Trim();
                        string mntMgn = eucKr.GetString(lineBytes, 369, 20).Trim();
                        string crcCd = eucKr.GetString(lineBytes, 389, 3).Trim();
                        string baseCrcCd = eucKr.GetString(lineBytes, 392, 3).Trim();
                        string counterCrcCd = eucKr.GetString(lineBytes, 395, 3).Trim();
                        string pipCost = eucKr.GetString(lineBytes, 398, 20).Trim();
                        string buyInt = eucKr.GetString(lineBytes, 418, 20).Trim();
                        string sellInt = eucKr.GetString(lineBytes, 438, 20).Trim();
                        string roundLots = eucKr.GetString(lineBytes, 458, 6).Trim();
                        string scaleChiper = eucKr.GetString(lineBytes, 464, 10).Trim();
                        string decimalchiper = eucKr.GetString(lineBytes, 474, 5).Trim();
                        string jnilVolume = eucKr.GetString(lineBytes, 479, 10).Trim();
                        try
                        {
                            Product product = FindProduct(productCode);

                            StockItem stockItem = new StockItem
                            {
                                ItemCode = symbolCode,
                                NameKr = symbolNameKr,
                                NameEn = symbolNameEn,
                                Decimal = Convert.ToInt32(pdesz),
                                SeungSu = Convert.ToDouble(mltiPler),
                                ContractSize = Convert.ToDouble(ctrtSize),
                                TickValue = Convert.ToDouble(tickValue),
                                TickSize = Convert.ToDouble(tickSize),
                                ExpireDay = exprDt,
                                PredayVolume = Convert.ToInt32(jnilVolume)
                            };

                            SaveItem(stockItem);
                            if (product != null)
                            {
                                product.AddItem(stockItem);
                                product.AddToYearMonth(1, stockItem.ItemCode, stockItem);
                                Debug.WriteLine(lineNum++);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.Log($"Line : {lineNum} logged at {symbolCode}");
                            MessageBox.Show($"Error iterating Item file: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading Item file: {ex.Message}");
            }
        }


        public void ReadABMarketFile(string subDirectory, string fileName)
        {
            try
            {
                // Get the application path
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                LogManager.Log($"Application path: {appPath}");

                // Combine the path to the subdirectory and file name
                string filePath = Path.Combine(appPath, subDirectory, fileName);
                LogManager.Log($"Full file path: {filePath}");

                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    LogManager.Log($"The file '{filePath}' does not exist.");
                    MessageBox.Show($"Error: The file '{filePath}' does not exist.");
                    return;
                }
                // Specify the euc-kr encoding
                int euckrCodePage = 51949;  // euc-kr code page
                Encoding eucKr = Encoding.GetEncoding(euckrCodePage);

                // Read the file line by line
                using (StreamReader sr = new StreamReader(filePath, eucKr))
                {
                    string line;
                    int lineNum = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        try
                        {
                            byte[] lineBytes = eucKr.GetBytes(line);

                            string market_type = eucKr.GetString(lineBytes, 0, 20).Trim();
                            string exchange = eucKr.GetString(lineBytes, 20, 5).Trim();
                            string pmCode = eucKr.GetString(lineBytes, 25, 3).Trim();
                            string enName = eucKr.GetString(lineBytes, 28, 50).Trim();
                            string name = eucKr.GetString(lineBytes, 78, 50).Trim();

                            Market market = AddMarket(market_type);
                            LogManager.Log($"Successfully added product at line {lineNum}");
                            Product product = market.FindAddProduct(pmCode);
                            try
                            {
                                product.MarketName = market_type;
                                product.Exchange = exchange;
                                product.Name = enName;
                                product.NameKr = name;
                                LogManager.Log("Product added successfully.");
                                AddProduct(product);
                                lineNum++;
                                LogManager.Log($"Successfully added product at line {lineNum}");
                                Debug.WriteLine(lineNum);
                            }
                            catch (Exception ex)
                            {
                                LogManager.Log($"Exception while adding product at line {lineNum}: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.Log($"Exception while processing line {lineNum}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"Error reading Market file: {ex.Message}");
                MessageBox.Show($"Error reading Market file: {ex.Message}");
            }
        }

        public Product FindProduct(string productCode)
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                throw new ArgumentException("Product code cannot be null or empty", nameof(productCode));
            }

            if (_products.TryGetValue(productCode, out var product))
            {
                return product;
            }
            else
            {
                // Debugging statement for missing key
                Console.WriteLine($"Product with ProductCode {productCode} not found in dictionary.");
                return null;
            }
        }

        public void AddProduct(Product product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.ProductCode))
            {
                throw new ArgumentException("Product or ProductCode cannot be null or empty");
            }

            try
            {
                // Debugging statement to check the ProductCode
                Console.WriteLine($"Adding product with ProductCode: {product.ProductCode}");
                _products[product.ProductCode] = product;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding product: {ex.Message}");
                throw;
            }
        }


        Market AddMarket(string name)
        {
            Market found_market = FindMarket(name);
            if (found_market != null)
                return found_market;

            Market market = new Market();
            market.Name = name;
            _marketList.Add(market);
            return market;
        }

        public Market FindMarket(string marketName)
        {
            return _marketList.FirstOrDefault(market => market.Name == marketName);
        }

        // Function to get items with a specific prefix and sort by ItemCode in ascending order
        public List<StockItem> GetItemsByProductCode(string productCodePrefix)
        {
            return _items.Values
                         .Where(item => item.ItemCode.StartsWith(productCodePrefix))
                         .OrderBy(item => item.ItemCode)
                         .ToList();
        }

        public StockItem GetLatestItemByProductCode(string productCodePrefix)
        {
            if (!_products.ContainsKey(productCodePrefix))
                return null;
            Product product;
           _products.TryGetValue(productCodePrefix, out product);
            if (product == null) return null;
            var yearMonth = product.GetRecentYearMonth();
            if (yearMonth == null) return null;
            var item = yearMonth.GetLatestStockItem();
            return item;
        }

        public List<StockItem> GetFavoriteItemsByProductCode()
        {
            List<StockItem> itemList = new List<StockItem>();
            foreach(var item in _FavoriteProductSet)
            {
                var stockItem = GetLatestItemByProductCode(item);
                if (stockItem == null) continue;
                itemList.Add(stockItem);
            }
            return itemList;
        }

        public StockItem GetFirstFavoriteItemsByProductCode()
        {
            List<StockItem> itemList = new List<StockItem>();
            foreach (var item in _FavoriteProductSet)
            {
                var stockItem = GetLatestItemByProductCode(item);
                if (stockItem == null) continue;
                itemList.Add(stockItem);
            }
            if (itemList.Count == 0) return null;
            return itemList[0];
        }

        // Function to initialize the product set
        public void InitProductSet()
        {
            _FavoriteProductSet.Add("NQ");
            _FavoriteProductSet.Add("MNQ");
            _FavoriteProductSet.Add("CL");
            _FavoriteProductSet.Add("HSI");
        }
    }
}