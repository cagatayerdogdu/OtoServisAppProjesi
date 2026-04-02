using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.Firebase.Core.Platforms.Android;
using Plugin.Firebase.Core; // AYARLAR İÇİN BU EKLENDİ

namespace OtoServisApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // YENİ EKLENEN METOT: Android uygulaması ayağa kalktığı salise Firebase'i başlatır
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            /* ESKİ KOD (MAUI Program.cs'e taşındığı ve native crash'i önlemek için yorum satırına alındı)
            // DİKKAT: Cloud Messaging (Bildirim) modülünü aktif ederek başlatıyoruz!
            //CrossFirebase.Initialize(this, new CrossFirebaseSettings(isCloudMessagingEnabled: true));
            try
            {
                // Firebase motorunu güvenli bir zırh içinde başlatıyoruz
                CrossFirebase.Initialize(this);
            }
            catch (Exception ex)
            {
                // Çökerse arka planda kalsın, uygulamayı kapatmasın
                Console.WriteLine($"Firebase Başlatılma Hatası: {ex.Message}");
            }
            */

            // Durum Çubuğu (Status Bar) rengini mor yerine turkuaz yapıyoruz.
            if (Window != null)
            {
                Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#00BCD4"));
            }
        }
    }
}