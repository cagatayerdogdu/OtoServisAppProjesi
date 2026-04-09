using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OtoServisApp.Models
{
    public class BildirimResponse : INotifyPropertyChanged
    {
        public int id { get; set; }
        public string baslik { get; set; }
        public string mesaj { get; set; }
        public bool okundu_mu { get; set; }
        public DateTime olusturulma_tarihi { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}