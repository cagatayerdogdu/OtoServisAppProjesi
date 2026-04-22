using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
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
            throw new InvalidOperationException("Aktif sayfa bulunamadı.");

        // Sayfanın kök Layout'unu al (Grid değilse Grid'e çevir)
        var rootLayout = GetOrCreateRootGrid(currentPage);

        var alertView = new ModernAlertView
        {
            ZIndex = 9999,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
            BackgroundColor = Colors.Transparent
        };

        // Grid ise tüm satır ve sütunları kapla
        if (rootLayout is Grid grid)
        {
            int rowCount = grid.RowDefinitions.Count;
            int colCount = grid.ColumnDefinitions.Count;

            if (rowCount > 0)
                Grid.SetRowSpan(alertView, rowCount);
            if (colCount > 0)
                Grid.SetColumnSpan(alertView, colCount);
        }

        rootLayout.Children.Add(alertView);

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
            if (rootLayout.Children.Contains(alertView))
                rootLayout.Children.Remove(alertView);
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

    private static Grid GetOrCreateRootGrid(Page page)
    {
        if (page is ContentPage contentPage)
        {
            // Zaten Grid ise direkt kullan					   
            if (contentPage.Content is Grid existingGrid)
                return existingGrid;

            // Mevcut içeriği Grid'e sar
            var grid = new Grid
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            var oldContent = contentPage.Content;
            contentPage.Content = grid;

            if (oldContent != null)
            {
                if (oldContent is View oldView)
                {
                    oldView.HorizontalOptions = LayoutOptions.Fill;
                    oldView.VerticalOptions = LayoutOptions.Fill;
                }
                grid.Children.Add(oldContent);
            }

            return grid;
        }

        throw new InvalidOperationException("Sayfa tipi ContentPage değil.");
    }

    /* OTP Doğrulama Kodu için Giriş Kutusu */
    public static Task<string?> ShowPromptAsync(string mesaj, string baslik = "Bilgi")
    {
        var tcs = new TaskCompletionSource<string?>();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var alertView = new ModernAlertView();
            var currentPage = GetCurrentPage();
            var rootGrid = GetOrCreateRootGrid(currentPage);
            rootGrid.Children.Add(alertView);
            var result = await alertView.ShowPromptAsync(mesaj, baslik);
            rootGrid.Children.Remove(alertView);
            tcs.SetResult(result);
        });
        return tcs.Task;
    }

    // Eski kodlar hata vermesin diye boş Initialize metodu										
    public static void Initialize(Page page) { }
}