using System;

namespace Exchange
{
    public class Order
    {
        public Order(Participant seller, string item, int price, TimeSpan timestamp)
        {
            Seller = seller;
            Item = item;
            Price = price;
            Timestamp = timestamp;
        }

        public Participant Seller { get; }
        public string Item { get; }
        public int Price { get; }
        public TimeSpan Timestamp { get; }
    }
}