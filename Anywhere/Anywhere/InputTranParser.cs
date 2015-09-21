using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

using StockSharp.Quik.Lua;
using StockSharp.Fix;
using StockSharp.Messages;
using StockSharp.BusinessEntities;
using StockSharp.Algo;


namespace StockSharp.Anywhere
{
    public class InputTranParser
    {
        private Timer _tmr;
        private int _tmrDelay = 1000;
        private string _tranFilePath = "InputCommands.txt";
        private long _lastTranId = 0;

        private IList<TransactionKey> _tranKeys;

        string _transIDpattern = @"TRANS_ID\s+=\s+\d|TRANS_ID=\s+\d|TRANS_ID=\d"; //  

        private Object _lock = new Object();

        LuaFixTransactionMessageAdapter _transAdapter;
        FixMessageAdapter _messAdapter;
        List<Security> _securities;

        public InputTranParser(LuaFixTransactionMessageAdapter transAdapter, FixMessageAdapter messAdapter, List<Security> securities)
        {
            _transAdapter = transAdapter;
            _messAdapter = messAdapter;
            _securities = securities;

            _tranKeys = new List<TransactionKey>();

            _tranKeys.Add(new TransIdKey());
            _tranKeys.Add(new AccountKey());
            _tranKeys.Add(new ClientCodeKey());
            _tranKeys.Add(new ClassCodeKey());
            _tranKeys.Add(new SecCodeKey());
            _tranKeys.Add(new ActionKey());
            _tranKeys.Add(new OperationKey());
            _tranKeys.Add(new PriceKey());
            _tranKeys.Add(new StopPriceKey());
            _tranKeys.Add(new QuantityKey());
            _tranKeys.Add(new TypeKey());
            _tranKeys.Add(new OrderKeyKey());
            _tranKeys.Add(new OriginalTransIdKey());
            _tranKeys.Add(new CommentKey());

            _tmr = new Timer(new TimerCallback(tmr_tick), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        }

        public void Start()
        {
            _tmr.Change(0, _tmrDelay);
        }

        public void Stop()
        {
            _tmr.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        private void tmr_tick(Object obj)
        {

            lock (_lock)
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        string[] lines = System.IO.File.ReadAllLines(_tranFilePath);

                        List<string> newlines = new List<string>();

                        if (_lastTranId > 0)
                        {
                            for (i = lines.Count() - 1; i >= 0; i--)
                            {
                                var id = GetTransId(lines[i]);
                                if (id > _lastTranId)
                                {
                                    newlines.Add(lines[i]);
                                }
                                else
                                {
                                    if (newlines.Count() == 0) return;
                                }
                            }

                            newlines.Reverse();
                        }
                        else
                        {
                            newlines.AddRange(lines);
                        }

                        NewLinesParsing(newlines);
                    }
                    catch (ArgumentException ex)
                    {
                        Debug.WriteLine(ex);
                        this.Stop();
                        return;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(10);
                    }
                }
            }

        }

        // teturn Trans_id from line 
        private long GetTransId(string line)
        {
            Match trmatch = Regex.Match(line, _transIDpattern, RegexOptions.IgnoreCase);
            if (trmatch.Success)
            {
                Match idmatch = Regex.Match(trmatch.Value, @"\d");
                if (idmatch.Success)
                {
                    return long.Parse(idmatch.Value);
                }
                else
                {
                    throw new ArgumentException("TRANS_ID value is not valid.");
                }
            }
            else
            {
                throw new ArgumentException("TRANS_ID key not found.");
            }
        }

        // command lines parsing
        private void NewLinesParsing(IList<string> lines)
        {
            foreach (var line in lines)
            {
                var temp = line.Split(';');

                var actions = new Dictionary<string, object>();

                foreach (var item in temp)
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;

                    var key = item.Split('=')[0].Trim();
                    var value = item.Split('=')[1].Trim();

                    var kvp = ConvertAndValidateCommandParams(key, value);

                    actions.Add(kvp.Key, kvp.Value);

                }

                if (actions.Count > 0)
                    SendCommand(actions);

                _lastTranId = (long)actions["TRANS_ID"];

            }

        }

        // validate and convert of command line values
        private KeyValuePair<string, object> ConvertAndValidateCommandParams(string key, string value)
        {
            TransactionKey tk = _tranKeys.FirstOrDefault(t => t.KeyWord == key.ToUpper());

            Debug.WriteLine(string.Format("key - {0}, value {1}", key, value));

            if (tk != null)
            {

                var tranValue = tk.GetValue(value);

                Debug.WriteLine(string.Format("value {0}", tranValue));

                if (tranValue != null)
                    return new KeyValuePair<string, object>(tk.KeyWord, tranValue);
            }

            throw new ArgumentException("Transaction key or value is not valid.");

        }

        // create and send message
        private void SendCommand(Dictionary<string, object> actions)
        {

            Message message = null;

            switch (actions["ACTION"].ToString())
            {
                case "NEW_ORDER":
                    {
                        message = new OrderRegisterMessage()
                        {
                            SecurityId = _securities.FirstOrDefault(s => s.Code == actions["SECCODE"].ToString() &&
                                                                         s.Class == actions["CLASSCODE"].ToString()).ToSecurityId(),
                            ClientCode = actions["CLIENTCODE"].ToString(),
                            PortfolioName = actions["ACCOUNT"].ToString(),
                            OrderType = (OrderTypes)actions["TYPE"],
                            Price = (decimal)actions["PRICE"],
                            Side = (Sides)actions["OPERATION"],
                            Volume = (decimal)actions["QUANTITY"],
                            TransactionId = _transAdapter.TransactionIdGenerator.GetNextId(),
                            Comment = actions["COMMENT"].ToString()
                        };
                    }
                    break;
                case "KILL_ORDER":
                    {
                        message = new OrderCancelMessage()
                        {
                            OrderId = (long)actions["ORDER_KEY"],
                            OriginalTransactionId = (long)actions["ORIGINAL_TRANS_ID"],
                            TransactionId = _transAdapter.TransactionIdGenerator.GetNextId(),
                        };
                    }
                    break;
                case "KILL_ALL_ORDERS":
                    {
                        message = new OrderGroupCancelMessage()
                        {
                            TransactionId = _transAdapter.TransactionIdGenerator.GetNextId()
                        };
                    }
                    break;
                case "MOVE_ORDERS":
                    {
                        //TODO
                    }
                    break;
                case "REGISTER_SECURITY":
                    {
                        //TODO
                    }
                    break;
                case "UNREGISTER_SECURITY":
                    {
                        //TODO
                    }
                    break;
                case "REGISTER_TRADES":
                    {
                        //TODO
                    }
                    break;
                case "UNREGISTER_TRADES":
                    {
                        //TODO
                    }
                    break;
                case "REGISTER_MARKETDEPTH":
                    {
                        //TODO
                    }
                    break;
                case "UNREGISTER_MARKETDEPTH":
                    {
                        //TODO
                    }
                    break;

                default:
                    break;
            }

            if (message != null)
                _transAdapter.SendInMessage(message);
        }

    }
}