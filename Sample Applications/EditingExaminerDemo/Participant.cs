using System.Collections.Generic;
using System.ComponentModel;

namespace Exchange
{
    public class Participant : INotifyPropertyChanged
    {
        public Participant(string name, int brick, int lumber, int wool)
        {
            Name = name;

            Inventory = new Dictionary<string, int>
            {
                ["Brick"] = brick,
                ["Lumber"] = lumber,
                ["Wool"] = wool
            };

            Balance = 50;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static IEnumerable<string> Items
        {
            get
            {
                yield return "Brick";
                yield return "Lumber";
                yield return "Wool";
            }
        }

        public string Name { get; }
        public Dictionary<string, int> Inventory { get; }
        public int Balance { get; set; }
        public int OutstandingOrders { get; set; }

        public void Refresh()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}