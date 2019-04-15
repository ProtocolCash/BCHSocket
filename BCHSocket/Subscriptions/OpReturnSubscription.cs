using BCHSocket.Util;

namespace BCHSocket.Subscriptions
{
    public class OpReturnSubscription : Subscription
    {
        public byte[] OpReturnPrefix { get; }

        public OpReturnSubscription(byte[] prefix) : base(SubscriptionType.opreturn)
        {
            OpReturnPrefix = prefix;
        }

        public override int CompareTo(object obj)
        {
            if (obj.GetType() != typeof(OpReturnSubscription))
                return 1;

            var compare = (OpReturnSubscription) obj;

            return ByteUtil.CompareByteArray(compare.OpReturnPrefix, OpReturnPrefix);
        }
    }
}