// StatItem.cs
// This class holds the current value, previous value, and calculated difference for a single statistic.

using System.ComponentModel;
using System.Windows.Media;

namespace FileFlow
{
    public class StatItem : INotifyPropertyChanged
    {
        private long _currentValue;
        public long CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                OnPropertyChanged(nameof(CurrentValue));
                UpdateDifference();
            }
        }

        private long _previousValue;
        public long PreviousValue
        {
            get => _previousValue;
            set
            {
                _previousValue = value;
                OnPropertyChanged(nameof(PreviousValue));
                UpdateDifference();
            }
        }

        public long Difference => CurrentValue - PreviousValue;
        public string DifferenceText => Difference > 0 ? $"(+{Difference})" : $"({Difference})";
        public Brush DifferenceColor => Difference > 0 ? Brushes.Green : Brushes.Red;
        public bool IsDifferent => Difference != 0;

        private void UpdateDifference()
        {
            OnPropertyChanged(nameof(Difference));
            OnPropertyChanged(nameof(DifferenceText));
            OnPropertyChanged(nameof(DifferenceColor));
            OnPropertyChanged(nameof(IsDifferent));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}