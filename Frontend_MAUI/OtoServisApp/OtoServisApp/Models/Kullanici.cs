using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtoServisApp.Models
{
    // Python'daki schemas.KullaniciGiris'in karşılığı
    public class LoginRequest
    {
        public string eposta { get; set; }
        public string sifre { get; set; }
    }

    // Güncelleme için API'ye göndereceğimiz nesne
    public class KullaniciUpdate
    {
        public string ad_soyad { get; set; }
        public string telefon { get; set; }
        public string adres { get; set; }
    }

    // Python'dan dönecek schemas.Kullanici nesnesinin karşılığı
    public class Kullanici
    {
        public int id { get; set; }
        public string ad_soyad { get; set; }
        public string eposta { get; set; }
        public string telefon { get; set; }
        public string adres { get; set; } // YENİ EKLENDİ
        public bool aktif_mi { get; set; }
        public DateTime kayit_tarihi { get; set; }
        public string rol { get; set; } // YENİ EKLENEN SATIR

        // Kullanıcının araçlarını tutacak liste
        public List<Arac> araclar { get; set; }
    }
}
