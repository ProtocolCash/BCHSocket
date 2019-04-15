using System;
using System.Linq;

namespace BCHSocket.Subscriptions
{
    public abstract class Subscription : IComparable
    {
        public SubscriptionType Type { get; private set; }

        protected Subscription(SubscriptionType subscriptionType)
        {
            Type = subscriptionType;
        }

        public enum SubscriptionType
        {
            address,
            opreturn,
            block
        }

        public abstract int CompareTo(object obj);
    }
}