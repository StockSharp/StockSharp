#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Anywhere.AnywherePublic
File: InputTranParser.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Anywhere
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Ecng.Collections;

    using Algo;
    using BusinessEntities;
    using Fix;
    using Messages;
    using Quik.Lua;

    public class InputTranParser
    {
	    private const char _endOfLine = '?'; // a line terminator
	    private readonly FixMessageAdapter _messAdapter;
        private readonly List<Security> _securities;
	    private const string _tranFilePath = "InputCommands.tri";

	    private readonly IList<TransactionKey> _tranKeys;

        private readonly LuaFixTransactionMessageAdapter _transAdapter;

	    private const string _transIDpattern = @"TRANS_ID\s+=\s+\d|TRANS_ID=\s+\d|TRANS_ID=\d"; //  

	    private readonly FileSystemWatcher _watcher;
        private long _lastTranId;

        public InputTranParser(LuaFixTransactionMessageAdapter transAdapter, FixMessageAdapter messAdapter, List<Security> securities)
        {
            _transAdapter = transAdapter;
            _messAdapter = messAdapter;
            _securities = securities;

            _watcher = new FileSystemWatcher(_tranFilePath, "*.tri");

            _tranKeys = new List<TransactionKey>
            {
                new TransIdKey(),
                new AccountKey(),
                new ClientCodeKey(),
                new ClassCodeKey(),
                new SecCodeKey(),
                new ActionKey(),
                new OperationKey(),
                new PriceKey(),
                new StopPriceKey(),
                new QuantityKey(),
                new TypeKey(),
                new OrderKeyKey(),
                new OriginalTransIdKey(),
                new CommentKey()
            };
        }

        public void Start()
        {
            _watcher.Changed += OnFileChanged;
        }

        public void Stop()
        {
            _watcher.Changed -= OnFileChanged;
        }

        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            try
            {
				string strings;

                using (var fs = new FileStream(_tranFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                    strings = sr.ReadToEnd();

                if (string.IsNullOrEmpty(strings))
                    return;

                var lines = strings.Split(_endOfLine);

                var newlines = new List<string>();

                if (_lastTranId > 0)
                {
                    for (var i = lines.Count() - 1; i >= 0; i--)
                    {
                        if (!lines[i].Contains(_endOfLine))
                            continue;

                        var id = GetTransId(lines[i]);

                        if (id > _lastTranId)
                            newlines.Add(lines[i]);
                        else
                        {
                            if (newlines.IsEmpty())
                                return;
                        }
                    }

                    newlines.Reverse();
                }
                else
                    newlines.AddRange(lines.Where(l => l.Contains(_endOfLine)));

                NewLinesParsing(newlines);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Stop();
            }
        }

        // teturn Trans_id from line 
        private long GetTransId(string line)
        {
            var trmatch = Regex.Match(line, _transIDpattern, RegexOptions.IgnoreCase);
            if (!trmatch.Success)
                throw new ArgumentException("TRANS_ID key not found.");
            var idmatch = Regex.Match(trmatch.Value, @"\d");
            if (idmatch.Success)
                return long.Parse(idmatch.Value);
            throw new ArgumentException("TRANS_ID value is not valid.");
        }

        // command lines parsing
        private void NewLinesParsing(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var temp = line.Split(';');

                var actions = new Dictionary<string, object>();

                foreach (var item in temp)
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;

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
            var tk = _tranKeys.FirstOrDefault(t => t.KeyWord == key.ToUpper());

            Debug.WriteLine("key - {0}, value {1}", key, value);

            if (tk == null)
                throw new ArgumentException("Transaction key or value is not valid.");
            var tranValue = tk.GetValue(value);

            Debug.WriteLine("value {0}", tranValue);

            if (tranValue != null)
                return new KeyValuePair<string, object>(tk.KeyWord, tranValue);

            throw new ArgumentException("Transaction key or value is not valid.");
        }

        // create and send message
        private void SendCommand(IReadOnlyDictionary<string, object> actions)
        {
            Message message = null;

            switch (actions["ACTION"].ToString())
            {
                case "NEW_ORDER":
                {
                    message = new OrderRegisterMessage
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
                    message = new OrderCancelMessage
                    {
                        OrderId = (long)actions["ORDER_KEY"],
                        OriginalTransactionId = (long)actions["ORIGINAL_TRANS_ID"],
                        TransactionId = _transAdapter.TransactionIdGenerator.GetNextId()
                    };
                }
                    break;
                case "KILL_ALL_ORDERS":
                {
                    message = new OrderGroupCancelMessage
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
            }

            if (message != null)
                _transAdapter.SendInMessage(message);
        }
    }
}