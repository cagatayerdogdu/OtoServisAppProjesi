using System;

namespace OtoServisApp.Models
{
    /// <summary>
    /// API'den gelen SistemBildirimleri verisi için C# Model Sınıfı.
    /// AÇIKLAMA: Kullanıcının uygulama içi bildirimlerinin detaylarını tutar.
    /// </summary>
    public class BildirimResponse
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
        public bool okundu_mu { get; set; }

        /// <summary>
        /// Bildirimin oluşturulduğu tarih ve saat
        /// </summary>
        public DateTime olusturulma_tarihi { get; set; }
    }
}