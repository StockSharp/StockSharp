using System.Collections.Generic;
using System.Linq;


using StockSharp.Messages;

namespace StockSharp.Anywhere
{

    public enum TransValueTypes
    {
        Decimal,
        Integer,
        Long,
        Boolean,
        String,
        PredefinedSet,
    }

    public abstract class TransactionKey
    {
        public TransactionKey() { }

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
            this.KeyWord = "CLASSCODE";
            this.ValueType = TransValueTypes.String;
        }

    }

    public class SecCodeKey : TransactionKey
    {
        public SecCodeKey()
        {
            this.KeyWord = "SECCODE";
            this.ValueType = TransValueTypes.String;
        }

    }

    public class ActionKey : TransactionKey
    {
        public ActionKey()
        {
            this.KeyWord = "ACTION";
            this.ValueType = TransValueTypes.PredefinedSet;
            PredefinedValues = new List<string>()
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
            this.KeyWord = "ACCOUNT";
            this.ValueType = TransValueTypes.String;
            this.IsRequired = true;
        }
    }

    public class ClientCodeKey : TransactionKey
    {
        public ClientCodeKey()
        {
            this.KeyWord = "CLIENT_CODE";
            this.ValueType = TransValueTypes.String;
            this.IsRequired = true;
        }
    }

    public class TypeKey : TransactionKey
    {
        public TypeKey()
        {
            this.KeyWord = "TYPE";
            this.ValueType = TransValueTypes.PredefinedSet;
            PredefinedValues = new List<string>()
            {
                "L",
                "M"
            };

        }

        public override object GetValue(string value)
        {
            var valValue = base.GetValue(value);

            if (valValue != null)
            {
                return valValue.ToString() == "M" ? OrderTypes.Market : OrderTypes.Limit;
            }
            return null;
        }

    }

    public class OperationKey : TransactionKey
    {
        public OperationKey()
        {
            this.KeyWord = "OPERATION";
            this.ValueType = TransValueTypes.PredefinedSet;
            PredefinedValues = new List<string>()
            {
                "S",
                "B"
            };

        }
        public override object GetValue(string value)
        {

            var valValue = base.GetValue(value);

            if (valValue != null)
            {
                return valValue.ToString() == "S" ? Sides.Sell : Sides.Buy;
            }
            return null;
        }

    }

    public class QuantityKey : TransactionKey
    {
        public QuantityKey()
        {
            this.KeyWord = "QUANTITY";
            this.ValueType = TransValueTypes.Decimal;
        }

    }

    public class PriceKey : TransactionKey
    {
        public PriceKey()
        {
            this.KeyWord = "PRICE";
            this.ValueType = TransValueTypes.Decimal;
        }
    }

    public class StopPriceKey : TransactionKey
    {
        public StopPriceKey()
        {
            this.KeyWord = "STOPPRICE";
            this.ValueType = TransValueTypes.Decimal;
        }

    }

    public class TransIdKey : TransactionKey
    {
        public TransIdKey()
        {
            this.KeyWord = "TRANS_ID";
            this.ValueType = TransValueTypes.Long;
            this.IsRequired = true;
        }

    }

    public class OriginalTransIdKey : TransactionKey
    {
        public OriginalTransIdKey()
        {
            this.KeyWord = "ORIGINAL_TRANS_ID";
            this.ValueType = TransValueTypes.Long;
        }

    }

    public class OrderKeyKey : TransactionKey
    {
        public OrderKeyKey()
        {
            this.KeyWord = "ORDER_KEY";
            this.ValueType = TransValueTypes.Long;
        }

    }

    public class CommentKey : TransactionKey
    {
        public CommentKey()
        {
            this.KeyWord = "COMMENT";
            this.ValueType = TransValueTypes.String;
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
