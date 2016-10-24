using System;
using System.Collections.Generic;
using System.Threading;

namespace CsNet.Dispatch
{
    /// <summary>
    /// 分发中心，使用生产者-消费者模型。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Dispatcher<T> where T : Task
    {
        protected Queue<T> m_taskQueue;
        private Semaphore m_producer;
        private Semaphore m_consumer;
        private int m_producerTimeout;
        private int m_consumerTimeout;
        private int m_capacity;

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
            m_producerTimeout = Timeout.Infinite;
            m_consumerTimeout = Timeout.Infinite;
        }

        /// <summary>
        /// 设置生产者等待超时时间
        /// </summary>
        /// <param name="milliseconds"></param>
        public void SetProducerTimeout(int milliseconds)
        {
            m_producerTimeout = milliseconds;
        }

        /// <summary>
        /// 设置消费者等待超时时间
        /// </summary>
        /// <param name="milliseconds"></param>
        public void SetConsumerTimeout(int milliseconds)
        {
            m_consumerTimeout = milliseconds;
        }

        public bool Produce(T task)
        {
            if (!m_producer.WaitOne(m_producerTimeout))
                return false;

            lock (m_taskQueue)
            {
                m_taskQueue.Enqueue(task);
            }
            m_consumer.Release();
            return true;
        }

        public int Produce(List<T> tasks, int startIndex)
        {
            if (!m_producer.WaitOne(m_producerTimeout))
                return 0;

            int count = 0;
            lock (m_taskQueue)
            {
                count = Math.Min(tasks.Count - startIndex, m_capacity - m_taskQueue.Count);
                if (count < 0)
                {
                    count = 0;
                }
                for (int i = 0; i < count; ++i)
                {
                    m_taskQueue.Enqueue(tasks[startIndex + i]);
                }
            }
            m_consumer.Release(count);

            return count;
        }

        public bool Consume(out T task)
        {
            task = null;

            if (!m_consumer.WaitOne(m_consumerTimeout))
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
