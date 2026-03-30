

namespace OtoServisApp.Models
{
    public class Arac
    {
        public int id { get; set; }
        public int sahip_id { get; set; }
        public int? marka_id { get; set; }
        public int? model_id { get; set; }
        public string ozel_marka { get; set; }
        public string ozel_model { get; set; }
        public int yil { get; set; }
        public string yakit_tipi { get; set; }
        public int kilometre { get; set; }

        // Ekranda göstermek için kullanacağımız özellik
        public string marka_model_yazi { get; set; } = "Yükleniyor...";
    }
}
