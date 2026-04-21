using CommunityToolkit.Maui.ApplicationModel;
using OtoServisApp.Services;

namespace OtoServisApp.Services;

public class NotificationBadgeService
{
    private readonly IBadge _badge;
    private readonly ApiService _apiService;
    private int _unreadCount;
    public int UnreadCount => _unreadCount;

    public NotificationBadgeService(IBadge badge, ApiService apiService)
    {
        _badge = badge;
        _apiService = apiService;
    }

    /// <summary>
    /// Okunmamış bildirim sayısını API'den çeker ve rozeti günceller.
    /// </summary>
    public async Task UpdateBadgeFromApiAsync(int kullaniciId)
    {
        try
        {
            _unreadCount = await _apiService.OkunmamisBildirimSayisiGetirAsync(kullaniciId);
            _badge.SetCount((uint)_unreadCount);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Rozet güncellenirken hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Rozeti manuel olarak artırır (yeni bildirim geldiğinde).
    /// </summary>
    public void IncrementBadge()
    {
        _unreadCount++;
        _badge.SetCount((uint)_unreadCount);
    }

    /// <summary>
    /// Rozeti manuel olarak azaltır (bildirim okunduğunda).
    /// </summary>
    public void DecrementBadge()
    {
        if (_unreadCount > 0)
        {
            _unreadCount--;
            _badge.SetCount((uint)_unreadCount);
        }
    }

    /// <summary>
    /// Rozeti sıfırlar (tüm bildirimler okunduğunda).
    /// </summary>
    public void ClearBadge()
    {
        _unreadCount = 0;
        _badge.SetCount(0);
    }
}