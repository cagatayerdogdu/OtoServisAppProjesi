namespace OtoServisApp.Models
{
    public class AracModel
    {
        public int id { get; set; }
        public string ad { get; set; }
        public int marka_id { get; set; }
    }

    public class Marka
    {
        public int id { get; set; }
        public string ad { get; set; }
        public List<AracModel> modeller { get; set; }
    }
}