namespace OtoServisApp.Models
{
    public class ServisTalebiFotograf
    {
        public int id { get; set; }
        public int talep_id { get; set; }
        public string dosya_yolu { get; set; }

        // YENİ: Python'dan gelen yolu, API linkimizle birleştirip resmi gösterecek tam linki oluşturuyoruz
        public string TamUrl => $"{Services.ApiConfig.BaseUrl}/{dosya_yolu.Replace("\\", "/")}";
    }
}