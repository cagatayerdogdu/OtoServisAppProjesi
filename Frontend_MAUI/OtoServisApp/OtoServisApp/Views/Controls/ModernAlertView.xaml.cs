using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace OtoServisApp.Views.Controls;

public partial class ModernAlertView : ContentView
{
    private TaskCompletionSource<bool?> _tcs;

    public ModernAlertView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Uyarıyı gösterir.
    /// </summary>
    /// <param name="baslik">Başlık (null verilirse gösterilmez)</param>
    /// <param name="mesaj">Mesaj</param>
    /// <param name="butonTipi">"Tamam", "EvetHayir", "EvetIptal", "SilVazgec"</param>
    /// <returns>True (Evet/Sil), False (Hayır/Vazgeç) veya null (Tamam)</returns>
    public Task<bool?> ShowAsync(string baslik, string mesaj, string butonTipi = "Tamam")
    {
        // Önceki işlem varsa temizle
        _tcs?.TrySetCanceled();
        _tcs = new TaskCompletionSource<bool?>();

        // Başlık
        if (!string.IsNullOrEmpty(baslik))
        {
            BaslikLabel.Text = baslik;
            BaslikLabel.IsVisible = true;
        }
        else
        {
            BaslikLabel.IsVisible = false;
        }

        MesajLabel.Text = mesaj;

        // Butonları hazırla
        TekButonBorder.IsVisible = false;
        ButonlarGrid.IsVisible = false;
        ButonlarGrid.Children.Clear();

        switch (butonTipi)
        {
            case "Tamam":
                TekButonBorder.IsVisible = true;
                TekButonLabel.Text = "Tamam";
                break;

            case "EvetHayir":
                ButonlarGrid.IsVisible = true;
                ButonEkle("Evet", true, "#4CAF50");
                ButonEkle("Hayır", false, "#F44336");
                break;

            case "EvetIptal":
                ButonlarGrid.IsVisible = true;
                ButonEkle("Evet", true, "#4CAF50");
                ButonEkle("İptal", null, "#9E9E9E");
                break;

            case "SilVazgec":
                ButonlarGrid.IsVisible = true;
                ButonEkle("Sil", true, "#F44336");
                ButonEkle("Vazgeç", false, "#9E9E9E");
                break;
        }

        IsVisible = true;
        return _tcs.Task;
    }

    private void ButonEkle(string metin, bool? sonuc, string renk)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb(renk),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            HeightRequest = 45,
            Padding = 0,
            InputTransparent = false
        };

        var label = new Label
        {
            Text = metin,
            TextColor = Colors.White,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            InputTransparent = true
        };

        border.Content = label;

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) =>
        {
            IsVisible = false;
            _tcs?.TrySetResult(sonuc);
        };
        border.GestureRecognizers.Add(tapGesture);

        ButonlarGrid.Children.Add(border);
        if (ButonlarGrid.Children.Count == 1)
            Grid.SetColumn(border, 0);
        else
            Grid.SetColumn(border, 1);
    }

    private void OnTekButonTapped(object sender, TappedEventArgs e)
    {
        IsVisible = false;
        _tcs?.TrySetResult(null);
    }

    // Arka plana tıklanınca hiçbir şey yapma (kapanmasın)
    // Eğer kapanmasını istersen buraya IsVisible = false ekleyebilirsin.
}