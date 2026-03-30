namespace OtoServisApp.Models
{
    public class ServisTalebi
    {
        public int id { get; set; }
        public int kullanici_id { get; set; }
        public int arac_id { get; set; }
        public int hizmet_id { get; set; }
        public string talep_tarihi { get; set; }
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

        // EKLENECEK SATIR: Önerilen tarih doluysa true, boşsa false döner
        public bool onerilen_tarih_var_mi => onerilen_tarih != null;

        // Ekranda göstermek için C# tarafında dolduracağımız yardımcı özellikler
        public string hizmet_adi { get; set; } = "Yükleniyor...";
        public string arac_adi { get; set; } = "Yükleniyor...";
    }
}