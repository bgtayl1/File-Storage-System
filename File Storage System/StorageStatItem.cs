// StorageStatItem.cs
// A specialized version of StatItem for handling storage size in GB (double).

using System.ComponentModel;
using System.Windows.Media;
using System;

namespace FileFlow
{
    public class StorageStatItem : INotifyPropertyChanged
    {
        private double _currentValue;
        public double CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                OnPropertyChanged(nameof(CurrentValue));
                UpdateDifference();
            }
        }

        private double _previousValue;
        public double PreviousValue
        {
            get => _previousValue;
            set
            {
                _previousValue = value;
                OnPropertyChanged(nameof(PreviousValue));
                UpdateDifference();
            }
        }

        public double Difference => CurrentValue - PreviousValue;
        public string DifferenceText => Difference > 0 ? $"(+{Difference:F3})" : $"({Difference:F3})";
        public Brush DifferenceColor => Difference > 0 ? Brushes.Green : Brushes.Red;
        public bool IsDifferent => Math.Abs(Difference) > 0.001;

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
