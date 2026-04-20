using OtoServisApp.Services;

namespace OtoServisApp.Models;
public class TamamlananIs
{
    public int Id { get; set; }
    public string Baslik { get; set; }
    public string Aciklama { get; set; }
    public string Etiket { get; set; }
    public string Tarih { get; set; }
    public string ResimUrl { get; set; }
    public DateTime OlusturulmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }

    // Tam URL oluşturmak için BaseUrl ile birleştirilecek
    public string TamResimUrl => ResimUrl?.StartsWith("http") == true ? ResimUrl : $"{ApiConfig.BaseUrl.TrimEnd('/')}{ResimUrl}";
}