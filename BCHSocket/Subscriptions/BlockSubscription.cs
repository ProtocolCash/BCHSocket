namespace BCHSocket.Subscriptions
{
    public class BlockSubscription : Subscription
    {
        public BlockSubscription() : base(SubscriptionType.block)
        {
        }

        public override int CompareTo(object obj)
        {
            if (obj.GetType() == typeof(BlockSubscription))
                return 0;

            return -1;
        }
    }
}