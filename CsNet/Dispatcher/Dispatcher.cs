using System;
using System.Collections.Generic;
using System.Threading;

namespace CsNet.Dispatcher
{
    /// <summary>
    /// 分发中心，使用生产者-消费者模型。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Dispatcher<T>
    {
        protected Queue<T> m_taskQueue;
        private Semaphore m_producer;
        private Semaphore m_consumer;
        private int m_capacity;
        private int m_produceTimeout;
        private int m_consumeTimeout;

        /// <summary>
        /// </summary>
        /// <param name="capacity">容量</param>
        /// <param name="initSize">初始队列容量</param>
        public Dispatcher(int capacity, int initSize)
        {
            m_capacity = capacity;
            m_taskQueue = new Queue<T>(initSize);
            m_producer = new Semaphore(capacity, capacity);
            m_consumer = new Semaphore(0, capacity);
            m_produceTimeout = Timeout.Infinite;
            m_consumeTimeout = Timeout.Infinite;
        }

        /// <summary>
        /// 设置生产者等待超时时间
        /// </summary>
        /// <param name="milliseconds"></param>
        public void SetProducterTimeout(int milliseconds)
        {
            m_produceTimeout = milliseconds;
        }

        /// <summary>
        /// 设置消费者等待超时时间
        /// </summary>
        /// <param name="milliseconds"></param>
        public void SetConsumerTimeout(int milliseconds)
        {
            m_consumeTimeout = milliseconds;
        }

        public bool Produce(T task)
        {
            if (!m_producer.WaitOne(m_produceTimeout))
                return false;

            lock (m_taskQueue)
            {
                m_taskQueue.Enqueue(task);
            }
            m_consumer.Release();
            return true;
        }

        public bool Consume(ref T task)
        {
            if (!m_consumer.WaitOne(m_consumeTimeout))
                return false;

            bool hasTask = false;
            lock (m_taskQueue)
            {
                if (m_taskQueue.Count > 0)
                {
                    task = m_taskQueue.Dequeue();
                    hasTask = true;
                }
            }

            if (hasTask)
            {
                m_producer.Release();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
