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

using System;
using System.Collections.Generic;
using System.Threading;

namespace BCHSocket.Consumer
{
    /// <summary>
    ///     A consumer thread that waits for queued tasks and does processing when tasks are queued
    /// </summary>
    public abstract class Consumer<T, T2> :  IDisposable
    {
        private readonly object _locker = new object();
        private readonly Queue<T> _tasks = new Queue<T>();
        private readonly Thread _worker;
        protected readonly Action<T2> CallbackAction;

        /// <summary>
        ///     Constructor
        ///     - Starts threaded consumer worker
        /// </summary>
        protected Consumer(Action<T2> callbackAction)
        {
            CallbackAction = callbackAction;
            // Start the worker thread
            _worker = new Thread(Work);
            _worker.Start();
        }

        /// <summary>
        ///     Consumer Disposal
        ///     - Tell the work queue to exit, wait, and cleanup
        /// </summary>
        public void Dispose()
        {
            // Signal the consumer to exit.
            EnqueueTask(default(T), 0);
            // Wait for the consumer's thread to finish.
            _worker.Join();
        }

        /// <summary>
        ///     EnqueueTask - Queues a new data package for the consumer
        /// </summary>
        /// <param name="data">byte array of the data package</param>
        /// <param name="frameCounter">the index of the task/frame</param>
        public void EnqueueTask(T data, long frameCounter)
        {
            lock (_locker)
            {
                _tasks.Enqueue(data);
                Monitor.Pulse(_locker);
            }
        }

        /// <summary>
        ///     Work - Performs the consumer work cycle
        ///     - passes data to abstract DoWork function
        /// </summary>
        private void Work()
        {
            while (true)
            {
                T data;

                lock (_locker)
                {
                    while (_tasks.Count == 0)
                        Monitor.Wait(_locker);

                    data = _tasks.Dequeue();
                }

                if (data.Equals(default(T))) return;

                var result = DoWork(data);
                if (result != null)
                    CallbackAction(result);
            }
        }

        /// <summary>
        ///     DoWork - abstract worker function
        ///     - handles the actual data processing as required
        /// </summary>
        /// <param name="data"></param>
        protected abstract T2 DoWork(T data);
    }
}