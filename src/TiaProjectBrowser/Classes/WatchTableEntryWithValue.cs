using System.ComponentModel;
using System.Runtime.CompilerServices;
using TiaFileFormat.Wrappers.Controller.WatchTable;

namespace TiaAvaloniaProjectBrowser.Classes
{
    public class WatchTableEntryWithValue : WatchTableEntry, INotifyPropertyChanged
    {
        public WatchTableEntryWithValue(WatchTableEntry w)
        {
            this.Name = w.Name;
            this.Address = w.Address;
            this.Format = w.Format;
            this.Comment = w.Comment;
            this.ModifyValue = w.ModifyValue;
        }

        private object? _value;
        public object? Value
        {
            get => _value;
            set {
                _value = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
