using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace OtoServisApp.Views.Controls;

public partial class ModernAlertView : ContentView
{
    private TaskCompletionSource<bool?> _tcs;
    private TaskCompletionSource<string?> _promptTcs;

    public ModernAlertView()
    {
        InitializeComponent();
        InputTransparent = false;
    }

    public Task<bool?> ShowAsync(string baslik, string mesaj, string butonTipi = "Tamam")
    {
        _tcs = new TaskCompletionSource<bool?>();
        MesajLabel.Text = string.IsNullOrEmpty(baslik) ? mesaj : $"{baslik}\n\n{mesaj}";
        EntryBorder.IsVisible = false;
        ShowButtons(butonTipi);
        IsVisible = true;
        return _tcs.Task;
    }

    public Task<string?> ShowPromptAsync(string mesaj, string baslik = "Bilgi")
    {
        _promptTcs = new TaskCompletionSource<string?>();
        MesajLabel.Text = string.IsNullOrEmpty(baslik) ? mesaj : $"{baslik}\n\n{mesaj}";
        EntryBorder.IsVisible = true;
        PromptEntry.Text = string.Empty;
        ShowButtons("Prompt");
        IsVisible = true;
        return _promptTcs.Task;
    }

    private void ShowButtons(string butonTipi)
    {
        TekButonBorder.IsVisible = false;
        ButonlarGrid.IsVisible = false;
        ButonlarGrid.Children.Clear();

        switch (butonTipi)
        {
            case "Tamam":
                TekButonBorder.IsVisible = true;
                TekButonLabel.Text = "Tamam";
                break;

            case "Prompt":
                ButonlarGrid.IsVisible = true;
                ButonEkle("Onayla", true, true);
                ButonEkle("İptal", false, false);
                break;

            case "EvetHayir":
                ButonlarGrid.IsVisible = true;
                ButonEkle("Evet", true, true);
                ButonEkle("Hayır", false, false);
                break;

            case "EvetIptal":
                ButonlarGrid.IsVisible = true;
                ButonEkle("Evet", true, true);
                ButonEkle("İptal", null, false);
                break;

            case "SilVazgec":
                ButonlarGrid.IsVisible = true;
                ButonEkle("Sil", true, false);
                ButonEkle("Vazgeç", false, true);
                break;
        }
    }

    private void ButonEkle(string metin, bool? sonuc, bool isPositive)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            HeightRequest = 45,
            Padding = 0,
            InputTransparent = false
        };

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
            if (_promptTcs != null)
            {
                _promptTcs.TrySetResult(sonuc == true ? PromptEntry.Text : null);
            }
            else
            {
                _tcs?.TrySetResult(sonuc);
            }
        };
        border.GestureRecognizers.Add(tapGesture);

        ButonlarGrid.Children.Add(border);
        Grid.SetColumn(border, ButonlarGrid.Children.Count - 1);
    }

    private void OnTekButonTapped(object sender, TappedEventArgs e)
    {
        IsVisible = false;
        _tcs?.TrySetResult(null);
    }

    private void OnBackgroundTapped(object sender, TappedEventArgs e)
    {
        // Boş bırakıyoruz, arka plana tıklamayı yut, hiçbir işlem yapma ve arka plana geçme.
    }
}

