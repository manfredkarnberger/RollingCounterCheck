using System.Collections.ObjectModel;
using RollingCounterCheck.Models;

namespace RollingCounterCheck.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<CanMessageRow> Messages { get; } = new();

        public double BusLoadPercent { get; set; }
        public int CanBaudrateKbps { get; set; } = 500;
        public bool LedVisible { get; set; }

        public CanMessageRow GetOrCreate(uint id)
        {
            foreach (var m in Messages)
                if (m.CanId == id)
                    return m;

            var row = new CanMessageRow(id);
            Messages.Add(row);
            return row;
        }
    }
}
