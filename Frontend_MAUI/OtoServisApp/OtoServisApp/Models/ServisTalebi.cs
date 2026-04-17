using System;
using System.ComponentModel; // YENİ EKLENDİ
using System.Runtime.CompilerServices; // YENİ EKLENDİ

namespace OtoServisApp.Models
{
    // Sınıfımıza INotifyPropertyChanged yeteneği kazandırdık
    public class ServisTalebi : INotifyPropertyChanged
    {
        public int id { get; set; }
        public int kullanici_id { get; set; }
        public int arac_id { get; set; }
        public int hizmet_id { get; set; }
        public string talep_tarihi { get; set; }
        //public DateTime? talep_tarihi { get; set; }
        public string adres { get; set; }
        public string notlar { get; set; }
        public string durum { get; set; }
        public DateTime? onerilen_tarih { get; set; }

        public string kullanici_ad_soyad { get; set; }
        public string kullanici_telefon { get; set; }
        public string arac_adi_tam { get; set; }
        public double tahmini_tutar { get; set; }

        public string randevu_tarihi { get; set; }
        public bool duzeltme_istendi_mi { get; set; }
        public string duzeltme_notu { get; set; }

        // Tarihlerin null gelme ihtimaline karşı nullable yaptık
        public DateTime? tamamlanma_tarihi { get; set; }
        public DateTime? silinme_tarihi { get; set; }
        public DateTime? guncelleme_tarihi { get; set; }
        public string iptal_eden_ad_soyad { get; set; } // İptal eden kişi bilgisi

        // EKLENECEK SATIR: Önerilen tarih doluysa true, boşsa false döner
        public bool onerilen_tarih_var_mi => onerilen_tarih != null;

        // Ekranda göstermek için C# tarafında dolduracağımız yardımcı özellikler
        public string hizmet_adi { get; set; } = "Yükleniyor...";
        public string arac_adi { get; set; } = "Yükleniyor...";

        // Fotoğraf olup olmadığını UI tarafında kontrol etmek için
        // FOTOĞRAF VAR MI (INotifyPropertyChanged destekli)
        private bool _foto_var_mi;
        public bool foto_var_mi
        {
            get => _foto_var_mi;
            set
            {
                if (_foto_var_mi != value)
                {
                    _foto_var_mi = value;
                    OnPropertyChanged();
                }
            }
        }

        // --- YENİ EKLENEN KISIM (MADDE 46 - DROPDOWN KONTROLÜ) ---
        private bool _dropdownAcikMi = false;
        public bool DropdownAcikMi
        {
            get => _dropdownAcikMi;
            set
            {
                if (_dropdownAcikMi != value)
                {
                    _dropdownAcikMi = value;
                    OnPropertyChanged(); // Değer değiştiğinde arayüze haber ver
                }
            }
        }

        // Arayüzü tetikleyecek olay yapılandırması
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // ---------------------------------------------------------
    }
}