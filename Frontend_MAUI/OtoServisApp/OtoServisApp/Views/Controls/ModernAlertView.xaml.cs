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

    public Task<bool?> ShowAsync(string baslik, string mesaj, string butonTipi = "Tamam")
    {
        try
        {
            _tcs?.TrySetCanceled();
            _tcs = new TaskCompletionSource<bool?>();

            // Başlık ve mesaj birleştiriliyor
            string tamMesaj = string.IsNullOrEmpty(baslik) ? mesaj : $"{baslik}\n\n{mesaj}";
            MesajLabel.Text = tamMesaj;

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
                    ButonEkle("Evet", true, isPositive: true);
                    ButonEkle("Hayır", false, isPositive: false);
                    break;

                case "EvetIptal":
                    ButonlarGrid.IsVisible = true;
                    ButonEkle("Evet", true, isPositive: true);
                    ButonEkle("İptal", null, isPositive: false);
                    break;

                case "SilVazgec":
                    ButonlarGrid.IsVisible = true;
                    ButonEkle("Sil", true, isPositive: false);  // "Sil" olumsuz kategoride
                    ButonEkle("Vazgeç", false, isPositive: false);
                    break;
            }

            IsVisible = true;
            return _tcs.Task;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ModernAlertView Hatası: {ex.Message}");
            return Task.FromResult<bool?>(null);
        }
    }

    private void ButonEkle(string metin, bool? sonuc, bool isPositive)
    {
        // Sabit arka plan rengi: #F8FAFC
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            HeightRequest = 45,
            Padding = 0,
            InputTransparent = false
        };

        // Yazı rengi: olumlu için #0EA5E9, olumsuz için #D32F2F
        Color textColor = isPositive ? Color.FromArgb("#0EA5E9") : Color.FromArgb("#D32F2F");

        var label = new Label
        {
            Text = metin,
            TextColor = textColor,
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
}