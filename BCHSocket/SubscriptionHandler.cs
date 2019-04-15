using System.Collections.Generic;
using System.Linq;
using BCHSocket.Subscriptions;
using BCHSocket.Util;
using BCHSocket.Websocket;

namespace BCHSocket
{
    public class SubscriptionHandler
    {
        private readonly Dictionary<IWebsocketConnection, List<Subscription>> 
            _allSockets = new Dictionary<IWebsocketConnection, List<Subscription>>();

        public IEnumerable<IWebsocketConnection> GetAllSockets()
        {
            foreach (var socket in _allSockets)
                yield return socket.Key;
        }

        public IEnumerable<IWebsocketConnection> GetSocketsWithSubscription(Subscription subscription)
        {
            if (subscription.Type == Subscription.SubscriptionType.opreturn)
            {
                foreach (var (key, value) in _allSockets)
                {
                    if (value.Where(x => x.Type == subscription.Type).Any(sub => ByteUtil.IsByteArrayPrefix(((OpReturnSubscription) subscription).OpReturnPrefix,
                        ((OpReturnSubscription) sub).OpReturnPrefix)))
                    {
                        yield return key;
                    }
                }
                
            }
            else
            {
                foreach (var (key, value) in _allSockets)
                {
                    if (value.Where(x => x.Type == subscription.Type).Any(sub => sub.CompareTo(subscription) == 0))
                    {
                        yield return key;
                    }
                }
            }
        }

        public void AddSocket(IWebsocketConnection socket)
        {
            _allSockets.Add(socket, new List<Subscription>());
        }

        public void RemoveSocket(IWebsocketConnection socket)
        {
            _allSockets.Remove(socket);
        }

        public void AddSubscription(IWebsocketConnection socket, Subscription subscription)
        {
            if (_allSockets.TryGetValue(socket, out var list))
                list.Add(subscription);
            else
                _allSockets.Add(socket, new List<Subscription> { subscription });
        }

        public bool RemoveSubscription(IWebsocketConnection socket, Subscription subscription)
        {
            return _allSockets.TryGetValue(socket, out var list) && list.Remove(subscription);
        }
    }
}