namespace OtoServisApp.Models
{
    public class LogResponse
    {
        public int toplam_kayit { get; set; }
        public int filtreli_kayit { get; set; }
        public int toplam_sayfa { get; set; }
        public int mevcut_sayfa { get; set; }
        public List<SistemLog> loglar { get; set; }
    }
}