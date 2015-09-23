namespace StockSharp.Anywhere
{
    using System.Collections.Generic;
    using System.Linq;

    using Messages;

    public enum TransValueTypes
    {
        Decimal,
        Integer,
        Long,
        Boolean,
        String,
        PredefinedSet
    }

    public abstract class TransactionKey
    {
        public string KeyWord { set; get; }

        public TransValueTypes ValueType { set; get; }

        public IEnumerable<string> PredefinedValues { set; get; }

        public bool IsRequired { set; get; }

        public bool IsValid { set; get; }

        public virtual object GetValue(string value)
        {
            switch (ValueType)
            {
                case TransValueTypes.Decimal:
                    return ConvertToDecimal(value);
                case TransValueTypes.Integer:
                    return ConvertToInteger(value);
                case TransValueTypes.Long:
                    return ConvertToLong(value);
                case TransValueTypes.Boolean:
                    break;
                case TransValueTypes.String:
                    return StringValidate(value);
                case TransValueTypes.PredefinedSet:
                    return PredefinedSetValidate(value);
                default:
                    return null;
            }
            return null;
        }

        private object PredefinedSetValidate(string value)
        {
            IsValid = PredefinedValues.Contains(value.Trim().ToUpper());
            return IsValid ? value : null;
        }

        private string StringValidate(string value)
        {
            IsValid = !string.IsNullOrWhiteSpace(value);
            return IsValid ? value : null;
        }

        private decimal? ConvertToDecimal(string value)
        {
            decimal decValue;

            IsValid = decimal.TryParse(value, out decValue);

            return IsValid ? (decimal?)decValue : null;
        }

        private int? ConvertToInteger(string value)
        {
            int intValue;

            IsValid = int.TryParse(value, out intValue);

            return IsValid ? (int?)intValue : null;
        }

        private long? ConvertToLong(string value)
        {
            long longValue;

            IsValid = long.TryParse(value, out longValue);

            return IsValid ? (long?)longValue : null;
        }
    }

    public class ClassCodeKey : TransactionKey
    {
        public ClassCodeKey()
        {
            KeyWord = "CLASSCODE";
            ValueType = TransValueTypes.String;
        }
    }

    public class SecCodeKey : TransactionKey
    {
        public SecCodeKey()
        {
            KeyWord = "SECCODE";
            ValueType = TransValueTypes.String;
        }
    }

    public class ActionKey : TransactionKey
    {
        public ActionKey()
        {
            KeyWord = "ACTION";
            ValueType = TransValueTypes.PredefinedSet;
            PredefinedValues = new List<string>
            {
                "NEW_ORDER",
                "NEW_NEG_DEAL",
                "NEW_REPO_NEG_DEA",
                "NEW_STOP_ORDER",
                "KILL_ORDER",
                "KILL_NEG_DEA",
                "KILL_STOP_ORDER",
                "KILL_ALL_ORDERS",
                "KILL_ALL_STOP_ORDERS",
                "KILL_ALL_NEG_DEALS",
                "KILL_ALL_FUTURES_ORDERS",
                "MOVE_ORDERS",
                "REGISTER_SECURITY",
                "UNREGISTER_SECURITY",
                "REGISTER_TRADES",
                "UNREGISTER_TRADES",
                "REGISTER_MARKETDEPTH",
                "UNREGISTER_MARKETDEPTH"
            };
        }
    }

    public class AccountKey : TransactionKey
    {
        public AccountKey()
        {
            KeyWord = "ACCOUNT";
            ValueType = TransValueTypes.String;
            IsRequired = true;
        }
    }

    public class ClientCodeKey : TransactionKey
    {
        public ClientCodeKey()
        {
            KeyWord = "CLIENT_CODE";
            ValueType = TransValueTypes.String;
            IsRequired = true;
        }
    }

    public class TypeKey : TransactionKey
    {
        public TypeKey()
        {
            KeyWord = "TYPE";
            ValueType = TransValueTypes.PredefinedSet;
            PredefinedValues = new List<string>
            {
                "L",
                "M"
            };
        }

        public override object GetValue(string value)
        {
            var valValue = base.GetValue(value);

            if (valValue != null)
                return valValue.ToString() == "M" ? OrderTypes.Market : OrderTypes.Limit;
            return null;
        }
    }

    public class OperationKey : TransactionKey
    {
        public OperationKey()
        {
            KeyWord = "OPERATION";
            ValueType = TransValueTypes.PredefinedSet;
            PredefinedValues = new List<string>
            {
                "S",
                "B"
            };
        }

        public override object GetValue(string value)
        {
            var valValue = base.GetValue(value);

            if (valValue != null)
                return valValue.ToString() == "S" ? Sides.Sell : Sides.Buy;
            return null;
        }
    }

    public class QuantityKey : TransactionKey
    {
        public QuantityKey()
        {
            KeyWord = "QUANTITY";
            ValueType = TransValueTypes.Decimal;
        }
    }

    public class PriceKey : TransactionKey
    {
        public PriceKey()
        {
            KeyWord = "PRICE";
            ValueType = TransValueTypes.Decimal;
        }
    }

    public class StopPriceKey : TransactionKey
    {
        public StopPriceKey()
        {
            KeyWord = "STOPPRICE";
            ValueType = TransValueTypes.Decimal;
        }
    }

    public class TransIdKey : TransactionKey
    {
        public TransIdKey()
        {
            KeyWord = "TRANS_ID";
            ValueType = TransValueTypes.Long;
            IsRequired = true;
        }
    }

    public class OriginalTransIdKey : TransactionKey
    {
        public OriginalTransIdKey()
        {
            KeyWord = "ORIGINAL_TRANS_ID";
            ValueType = TransValueTypes.Long;
        }
    }

    public class OrderKeyKey : TransactionKey
    {
        public OrderKeyKey()
        {
            KeyWord = "ORDER_KEY";
            ValueType = TransValueTypes.Long;
        }
    }

    public class CommentKey : TransactionKey
    {
        public CommentKey()
        {
            KeyWord = "COMMENT";
            ValueType = TransValueTypes.String;
        }
    }
}

//"REGISTER_ORDER",
//"CANCEL_ORDER",
//"REREGISTER_ORDER",
//"REGISTER_STOP_ORDER",
//"CANCEL_STOP_ORDER",
//"CANCEL_ALL_ORDERS",
//"CANCEL_ALL_STOP_ORDERS",
//"REGISTER_SECURITY",
//"UNREGISTER_SECURITY",
//"REGISTER_TRADES",
//"UNREGISTER_TRADES",
//"REGISTER_MARKETDEPTH",
//"UNREGISTER_MARKETDEPTH"