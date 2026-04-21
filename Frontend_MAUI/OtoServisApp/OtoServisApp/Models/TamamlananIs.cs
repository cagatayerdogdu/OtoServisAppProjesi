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
    public int? HizmetId { get; set; }
    public DateTime OlusturulmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }

    //public string TamResimUrl => $"{ApiConfig.BaseUrl.TrimEnd('/')}{ResimUrl}";

    public string TamResimUrl => $"{ApiConfig.BaseUrl.TrimEnd('/')}/{ResimUrl?.TrimStart('/').Replace("\\", "/")}";

    public ImageSource? ResimSource { get; set; }
}