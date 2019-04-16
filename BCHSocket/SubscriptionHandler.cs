/*
 * Copyright (c) 2019 ProtocolCash
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 */

using System.Collections.Generic;
using System.Linq;
using BCHSocket.Subscriptions;
using BCHSocket.Websocket;

namespace BCHSocket
{
    /// <summary>
    ///     Tracks websocket connections and associated subscriptions
    /// </summary>
    public class SubscriptionHandler
    {
        private readonly Dictionary<IWebsocketConnection, List<Subscription>> 
            _allSockets = new Dictionary<IWebsocketConnection, List<Subscription>>();
        private readonly object _locker = new object();

        /// <summary>
        ///     Returns all active websocket connections
        /// </summary>
        /// <returns>all active websocket connections</returns>
        public IEnumerable<IWebsocketConnection> GetAllSockets()
        {
            lock (_locker)
                foreach (var socket in _allSockets)
                    yield return socket.Key;
        }

        /// <summary>
        ///     Returns all active websocket connections that have a subscription that matches the given testSubscription
        /// </summary>
        /// <param name="testSubscription">subscription for which to check</param>
        /// <returns>matching websocket connections</returns>
        public IEnumerable<IWebsocketConnection> GetSocketsWithSubscription(Subscription testSubscription)
        {
            lock (_locker)
            {
                foreach (var (socketConnection, subscriptions) in _allSockets)
                {
                    if (subscriptions.Where(x => x.Type == testSubscription.Type)
                        .Any(sub => testSubscription.CompareTo(sub) == 0))
                    {
                        yield return socketConnection;
                    }
                }
            }
        }

        /// <summary>
        ///     Add a new websocket connection
        /// </summary>
        /// <param name="socket"></param>
        public void AddSocket(IWebsocketConnection socket)
        {
            lock (_locker)
                _allSockets.Add(socket, new List<Subscription>());
        }

        /// <summary>
        ///     Remove a websocket connection
        /// </summary>
        /// <param name="socket"></param>
        public void RemoveSocket(IWebsocketConnection socket)
        {
            lock (_locker)
                _allSockets.Remove(socket);
        }

        /// <summary>
        ///     Add a new subscription to the given socket
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="subscription"></param>
        public void AddSubscription(IWebsocketConnection socket, Subscription subscription)
        {
            lock (_locker)
            {
                if (_allSockets.TryGetValue(socket, out var list))
                    list.Add(subscription);
                else
                    _allSockets.Add(socket, new List<Subscription> {subscription});
            }
        }

        /// <summary>
        ///     Remove existing subscription from the given socket
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public bool RemoveSubscription(IWebsocketConnection socket, Subscription subscription)
        {
            lock (_locker)
                return _allSockets.TryGetValue(socket, out var list) && list.Remove(subscription);
        }
    }
}