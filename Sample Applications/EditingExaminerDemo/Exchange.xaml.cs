using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Exchange
{
    public partial class Exchange : INotifyPropertyChanged
    {
        public Exchange()
        {
            InitializeComponent();

            _clock = new Clock(OnTick);

            Participants = new ObservableCollection<Participant>
            {
                new Participant("Gregg", RandomZeroToTen(), RandomZeroToTen(), RandomZeroToTen()),
                new Participant("Charlie", RandomZeroToTen(), RandomZeroToTen(), RandomZeroToTen()),
                new Participant("Chuck", RandomZeroToTen(), RandomZeroToTen(), RandomZeroToTen()),
            };

            Sales = new ObservableCollection<Order>();
            BindingOperations.EnableCollectionSynchronization(Sales, Sales);

            Log = new ObservableCollection<string>();
            BindingOperations.EnableCollectionSynchronization(Log, Log);

            this.DataContext = this;

            this.Loaded += OnLoad;

            int RandomZeroToTen() => _rnd.Next(0, 10);
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            OpenExchange();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TimeSpan Time
        {
            get => _time;
            private set
            {
                _time = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Participant> Participants { get; }

        public ObservableCollection<Order> Sales { get; }

        public ObservableCollection<string> Log { get; }

        private void OnTick()
        {
            Time += TimeSpan.FromMinutes(1);
            ClearExpiredOrders();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            OpenExchange();
        }

        private async void OpenExchange()
        {
            this.ButtonStart.IsEnabled = false;

            _clock.Start();

            List<Task> tasks = new List<Task>();
            foreach (var participant in Participants)
            {
                tasks.Add(Task.Run(() => RunParticipant(participant, _rnd)));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.ButtonStart.IsEnabled = true;
            }
        }

        private void RunParticipant(Participant participant, Random rnd)
        {
            Thread.CurrentThread.Name = $"Cat {participant.Name}";

            var perceivedValue = new Dictionary<string, int>();

            while (true)
            {
                foreach (var item in Participant.Items)
                {
                    perceivedValue[item] = rnd.Next(1, 10);
                }

                foreach (var item in Participant.Items)
                {
                    var desire = _rnd.Next(0, 4);

                    if (desire == 0)
                    {
                        if (participant.Inventory[item] == 0)
                        {
                            continue;
                        }

                        var order = FindOrders(item).FirstOrDefault(o => o.Seller == participant && o.Item == item);

                        if (order != null)
                        {
                            continue;
                        }

                        Sell(item, participant, perceivedValue[item]);
                    }
                    else if (desire == 3)
                    {
                        var orders = FindOrders(item);

                        var bestOrders = orders.Where(o => o.Seller != participant && o.Price <= participant.Balance).OrderBy(o => o.Price);

                        foreach (var order in bestOrders)
                        {
                            if (Buy(order, participant))
                            {
                                break;
                            }
                        }
                    }
                }

                _clock.WaitForNextCycle();
            }
        }

        private void ClearExpiredOrders()
        {
            lock (Sales)
            {
                var expiredOrders = Sales.Where(o => (Time - o.Timestamp).TotalMinutes > 5).ToList();

                foreach (var order in expiredOrders)
                {
                    lock (order.Seller)
                    {
                        Sales.Remove(order);
                        order.Seller.OutstandingOrders -= 1;
                    }
                }
            }
        }

        private IReadOnlyList<Order> FindOrders(string item)
        {
            lock (Sales)
            {
                return Sales.Where(o => o.Item == item).ToList();
            }
        }

        private void Sell(string item, Participant seller, int price)
        {
            lock (seller)
            {
                if (seller.OutstandingOrders < 2)
                {
                    lock (Sales)
                    {
                        Sales.Add(new Order(seller, item, price, Time));
                        seller.OutstandingOrders += 1;
                    }
                }
            }
        }

        private bool Buy(Order order, Participant buyer)
        {
            lock (order.Seller)
            {
                _clock.WaitForNextCycle();

                lock (buyer)
                {
                    _clock.WaitForNextCycle();

                    lock (Sales)
                    {
                        if (!Sales.Contains(order))
                        {
                            return false;
                        }
                    }

                    if (order.Seller.Inventory[order.Item] < 1 || buyer.Balance < order.Price)
                    {
                        return false;
                    }

                    buyer.Balance -= order.Price;
                    order.Seller.Balance += order.Price;
                    buyer.Inventory[order.Item] += 1;
                    order.Seller.Inventory[order.Item] -= 1;

                    lock (Sales)
                    {
                        Sales.Remove(order);
                        order.Seller.OutstandingOrders -= 1;
                    }

                    buyer.Refresh();
                    order.Seller.Refresh();

                    lock (Log)
                    {
                        Log.Add($"{buyer.Name} bought {order.Item} from {order.Seller.Name} for ${order.Price}");
                    }

                    return true;
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly Clock _clock;
        private Random _rnd = new Random();
        private TimeSpan _time;
    }
}
