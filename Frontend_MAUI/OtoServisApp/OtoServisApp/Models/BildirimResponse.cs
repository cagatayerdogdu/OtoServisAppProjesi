using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OtoServisApp.Models
{
    /// <summary>
    /// API'den gelen SistemBildirimleri verisi için C# Model Sınıfı.
    /// AÇIKLAMA: Kullanıcının uygulama içi bildirimlerinin detaylarını tutar.
    /// </summary>
    // Sınıfına INotifyPropertyChanged arayüzünü (interface) ekleyelim. Bildirimleri okuyunca yazı düzelsin.
    public class BildirimResponse : INotifyPropertyChanged
    {
        /// <summary>
        /// Bildirimin benzersiz numarası [PK]
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// Bildirimin kısa başlığı
        /// </summary>
        public string baslik { get; set; }

        /// <summary>
        /// Bildirimin detaylı mesaj içeriği
        /// </summary>
        public string mesaj { get; set; }

        /// <summary>
        /// Kullanıcının bildirimi okuyup okumadığı bilgisi
        /// </summary>
        // public bool okundu_mu { get; set; }

        /// <summary>
        /// Bildirimin oluşturulduğu tarih ve saat
        /// </summary>
        public DateTime olusturulma_tarihi { get; set; }

        // DÜZELTME: okundu_mu özelliği değiştiğinde UI'ı tetikleyecek yapı
        private bool _okundu_mu;
        public bool okundu_mu
        {
            get => _okundu_mu;
            set
            {
                if (_okundu_mu != value)
                {
                    _okundu_mu = value;
                    OnPropertyChanged(); // UI'a "Fontu güncelle" emrini verir
                }
            }
        }

        // INotifyPropertyChanged Zorunlu Uygulaması
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}