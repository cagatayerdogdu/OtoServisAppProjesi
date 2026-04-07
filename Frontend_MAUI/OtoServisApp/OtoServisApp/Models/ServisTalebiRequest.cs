using System;

namespace OtoServisApp.Models
{
    public class ServisTalebiRequest
    {
        public int kullanici_id { get; set; }
        public int arac_id { get; set; }
        public int hizmet_id { get; set; }
        public string talep_tarihi { get; set; } // "YYYY-MM-DD" formatında göndereceğiz
        public string adres { get; set; }
        public string notlar { get; set; }
        public DateTime? tamamlanma_tarihi { get; set; }
        public DateTime? silinme_tarihi { get; set; }
    }
}