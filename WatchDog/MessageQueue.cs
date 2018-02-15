using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Watchdog
{
    public class Message
    {
        public DateTime TimeStamp { get; set; }
        public uint Number { get; set; }
        public string Text { get; set; }

        public Message(uint number, string text)
        {
            TimeStamp = DateTime.Now;
            Number = number;
            Text = text;
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", TimeStamp, Number, Text);
        }

        public Shared.WatchdogMsgInfo ToWatchdogMessageInfo()
        {
            return new Shared.WatchdogMsgInfo(Utils.HardwareId, TimeStamp.ToString("u"), Number, Text);
        }

    }

    public class MessageQueue
    {
        Queue<Message> queue;
        int capacity;
        uint messageNumber = 0;

        public MessageQueue(int capacity = 100)
        {
            queue = new Queue<Message>(capacity);
            this.capacity = capacity;
        }

        public int Count
        {
            get { return queue.Count; }
        }

        public Message Dequeue()
        {
            return queue.Dequeue();
        }

        public Message Peek()
        {
            return queue.Peek();
        }

        public string AddMessage(string text)
        {
            while (queue.Count >= capacity)
                queue.Dequeue();

            queue.Enqueue(new Message(++messageNumber, text));

            return this.ToString();
        }

        public override string ToString()
        {
            string ret = "";

            lock (this)
            {
                for (int index = 0; index < queue.Count; index++)
                {
                    ret += queue.ElementAt(index).ToString() + "\n";
                }
            }

            return ret;
        }
    }
}
