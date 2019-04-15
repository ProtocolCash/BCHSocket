using BCHSocket.Util;
using SharpBCH.CashAddress;

namespace BCHSocket.Subscriptions
{
    public class AddressSubscription : Subscription
    {
        public DecodedBitcoinAddress DecodedAddress { get; }

        public AddressSubscription(DecodedBitcoinAddress decodedAddress) : base(SubscriptionType.address)
        {
            DecodedAddress = decodedAddress;
        }

        public override int CompareTo(object obj)
        {
            if (obj.GetType() != typeof(AddressSubscription))
                return 1;

            var compare = (AddressSubscription) obj;

            if (compare.DecodedAddress.Type != DecodedAddress.Type)
                return -1;

            if (compare.DecodedAddress.Prefix != DecodedAddress.Prefix)
                return -1;

            return ByteUtil.CompareByteArray(compare.DecodedAddress.Hash, DecodedAddress.Hash);
        }
    }
}