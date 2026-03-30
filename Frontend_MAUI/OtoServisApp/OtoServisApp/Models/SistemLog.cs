namespace OtoServisApp.Models
{
    public class SistemLog
    {
        public int id { get; set; }
        public string kullanici_ad_soyad { get; set; }
        public string seviye { get; set; }
        public string islem { get; set; }
        public string detay { get; set; }
        public string tarih { get; set; }
    }
}