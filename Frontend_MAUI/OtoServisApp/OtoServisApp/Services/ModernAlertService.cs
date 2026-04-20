using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using OtoServisApp.Views.Controls;

namespace OtoServisApp.Services;

public static class ModernAlertService
{
    public static async Task<bool?> ShowAsync(string baslik, string mesaj, string butonTipi = "Tamam")
    {
        if (!MainThread.IsMainThread)
            return await MainThread.InvokeOnMainThreadAsync(() => ShowAsync(baslik, mesaj, butonTipi));

        var currentPage = GetCurrentPage();
        if (currentPage == null)
            throw new InvalidOperationException("ModernAlertService: Aktif sayfa bulunamadı.");

        // Sayfanın en üst katmanına erişmek için AbsoluteLayout kullan
        var overlay = new AbsoluteLayout
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = false,
            ZIndex = 9999
        };

        var alertView = new ModernAlertView
        {
            InputTransparent = false
        };

        // AlertView'i AbsoluteLayout'un tam ortasına yerleştir
        AbsoluteLayout.SetLayoutFlags(alertView, AbsoluteLayoutFlags.PositionProportional);
        AbsoluteLayout.SetLayoutBounds(alertView, new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));

        overlay.Children.Add(alertView);

        // Sayfaya overlay'i ekle
        var pageRoot = GetPageRoot(currentPage);
        pageRoot.Children.Add(overlay);

        try
        {
            var result = await alertView.ShowAsync(baslik, mesaj, butonTipi);
            return result;
        }
        catch (Exception ex)
        {
            // Herhangi bir hata olursa uygulama çökmesin, loglansın
            System.Diagnostics.Debug.WriteLine($"ModernAlertService Hatası: {ex.Message}");
            return null;
        }
        finally
        {
            if (pageRoot.Children.Contains(overlay))
                pageRoot.Children.Remove(overlay);
        }
    }

    public static Task ShowInfoAsync(string mesaj, string baslik = "Bilgi")
        => ShowAsync(baslik, mesaj, "Tamam");

    public static async Task<bool> ShowConfirmationAsync(string mesaj, string baslik = "Onay")
    {
        var result = await ShowAsync(baslik, mesaj, "EvetHayir");
        return result == true;
    }

    public static async Task<bool?> ShowDeleteConfirmationAsync(string mesaj, string baslik = "Silme Onayı")
    {
        return await ShowAsync(baslik, mesaj, "SilVazgec");
    }

    // ========== YARDIMCI METODLAR ==========												
    private static Page GetCurrentPage()
    {
        var mainPage = Application.Current?.MainPage;
        if (mainPage == null) return null;

        if (mainPage is NavigationPage navPage)
            return navPage.CurrentPage ?? mainPage;

        if (mainPage is TabbedPage tabbedPage)
        {
            var currentTab = tabbedPage.CurrentPage;
            if (currentTab is NavigationPage innerNav)
                return innerNav.CurrentPage ?? currentTab;
            return currentTab;
        }

        if (mainPage is FlyoutPage flyout)
            return flyout.Detail;

        return mainPage;
    }

    private static Layout GetPageRoot(Page page)
    {
        if (page is ContentPage contentPage)
        {
            // Eğer sayfa içeriği Layout değilse (örneğin tek bir Label), onu bir Grid'e sar
            if (contentPage.Content is not Layout layout)
            {
                var grid = new Grid();
                var oldContent = contentPage.Content;
                contentPage.Content = grid;
                if (oldContent != null)
                    grid.Children.Add(oldContent);
                return grid;
            }
            return layout;
        }

        // Fallback (normalde ContentPage olmayan sayfalar için)
        throw new InvalidOperationException("Sayfa kök Layout alınamadı.");
    }

    // Eski kodlar hata vermesin diye boş Initialize metodu			
    public static void Initialize(Page page) { }
}