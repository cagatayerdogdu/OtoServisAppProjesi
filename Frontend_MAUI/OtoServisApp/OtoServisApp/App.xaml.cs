using Plugin.Firebase.CloudMessaging;
using System.Text;
using OtoServisApp.Models;
using OtoServisApp.Views;
using System.Diagnostics;
using OtoServisApp.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OtoServisApp
{
    public partial class App : Application
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public App()
        {
            // === GLOBAL EXCEPTION HANDLER (BAŞLANGIÇ) ===
            /*AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                LogException(ex, "UnhandledException");
            };
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogException(e.Exception, "UnobservedTaskException");
                e.SetObserved();
            };*/
            // === GLOBAL EXCEPTION HANDLER (BİTİŞ) ===

            // Global exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            InitializeComponent();

            // MainPage = new AppShell();
            // MainPage = new Views.LoginView(); // Kendi yazdığımız login ekranına girmesini sağlıyourz.
            // MainPage = new NavigationPage(new Views.LoginView());
            var navPage = new NavigationPage(new LoginView());

            // Üst barın arka plan rengini turkuaz (Primary), yazı rengini beyaz yapıyoruz
            navPage.BarBackgroundColor = Color.FromArgb("#00BCD4");
            navPage.BarTextColor = Colors.White;
            MainPage = navPage;

            ModernAlertService.Initialize(MainPage);

            // UYGULAMA AÇIKKEN GELEN BİLDİRİMLERİ EKRANDA GÖSTERME KODU
            CrossFirebaseCloudMessaging.Current.NotificationReceived += async (sender, args) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Rozeti artır
                    var badgeService = Handler?.MauiContext?.Services.GetService<NotificationBadgeService>();
                    badgeService?.IncrementBadge();

                    // Uygulama içinde ModernAlertService ile bildirimi göster
                    await ModernAlertService.ShowInfoAsync(args.Notification.Body, args.Notification.Title);

                    // (Opsiyonel) Android'de sistem tepsisine de düşürmek için:
#if ANDROID
            var context = Android.App.Application.Context;
            var notificationManager = (Android.App.NotificationManager)context.GetSystemService(Android.Content.Context.NotificationService);
            var channelId = "foreground_channel";
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var channel = new Android.App.NotificationChannel(channelId, "Foreground Notifications", Android.App.NotificationImportance.High);
                notificationManager?.CreateNotificationChannel(channel);
            }

            var builder = new Android.App.Notification.Builder(context, channelId)
                .SetContentTitle(args.Notification.Title)
                .SetContentText(args.Notification.Body)
                //.SetSmallIcon(Resource.Drawable.notification_icon)
                // YENİ (Mevcut uygulama simgesi)
                .SetSmallIcon(Resource.Drawable.notification_icon)   // veya appicon, ic_launcher vb.
                .SetAutoCancel(true);

            notificationManager?.Notify(new Random().Next(1000, 9999), builder.Build());
#endif
                });
            };

            CrossFirebaseCloudMessaging.Current.NotificationTapped += (sender, args) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (Application.Current?.MainPage is TabbedPage tabbedPage)
                    {
                        var currentNav = tabbedPage.CurrentPage as NavigationPage;
                        if (currentNav != null)
                        {
                            await currentNav.Navigation.PushAsync(new NotificationsView());
                        }
                    }
                    else if (Application.Current?.MainPage is NavigationPage navPage)
                    {
                        await navPage.Navigation.PushAsync(new NotificationsView());
                    }
                });
            };

            // Uygulama başlangıcında SecureStorage anahtarlarını kontrol et ve bozuksa temizle
            //Task.Run(async () => await SecureStorageHelper.CleanupCorruptedKeysAsync());
        }
        /*
        private void LogException(Exception ex, string type)
        {
            try
            {
                var logPath = Path.Combine(FileSystem.AppDataDirectory, "crash.log");
                var logText = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {ex?.Message}\n{ex?.StackTrace}\n\n";
                File.AppendAllText(logPath, logText);
                System.Diagnostics.Debug.WriteLine(logText);
            }
            catch {  
                    //Log yazılamazsa sessiz geç  
                  }
        }
        */
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogExceptionToBackend(ex, "UnhandledException");
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogExceptionToBackend(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        }

        private async void LogExceptionToBackend(Exception ex, string type)
        {
            try
            {
                var errorData = new
                {
                    message = $"{type}: {ex?.Message}",
                    stack_trace = ex?.StackTrace,
                    source = "App.xaml.cs"
                };
                var json = System.Text.Json.JsonSerializer.Serialize(errorData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                // Ngrok adresi
                //await _httpClient.PostAsync("https://runny-scrutinizingly-ela.ngrok-free.dev/api/log-client-error", content);

                // YENİ REVİZE: Ngrok yerine ApiConfig üzerinden dinamik URL alıyoruz
                // (Eğer ApiConfig hata verirse en yukarıya "using OtoServisApp.Services;" eklemeyi unutma)
                string apiUrl = $"{ApiConfig.BaseUrl}/api/log-client-error";
                await _httpClient.PostAsync(apiUrl, content);
            }
            catch (Exception)
            {
                Debug.WriteLine($"LogExceptionToBackend__App_xaml: {ex.Message}");
            }

            // Ayrıca cihazdaki crash.log dosyasına da yaz
            try
            {
                var logPath = Path.Combine(FileSystem.AppDataDirectory, "OtoServisBakim_crash.log");
                var logText = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {ex?.Message}\n{ex?.StackTrace}\n\n";
                File.AppendAllText(logPath, logText);
            }
            catch { Debug.WriteLine($"LogExceptionToBackend__App_xaml_CrashLog: {ex?.Message}"); }
        }

        // App.xaml.cs içindeki Login başarılı olduktan sonraki yönlendirme:
        public void NavigateToMainTabbedPage(Kullanici kullanici)
        {
            // Sayfaların alt alta butonlarla değil, altta şık ikonlarla görünmesi için:
            //MainPage = new MainTabbedPage(kullanici);
            var mainTabbedPage = new MainTabbedPage(kullanici);
            MainPage = mainTabbedPage;
            // Başına Views. ekleyerek tam yolunu gösteriyoruz en üsste using kullandım alttakinin yerine
            //MainPage = new Views.MainTabbedPage(kullanici);

            // ModernAlertService'i yeni ana sayfaya bağla
            ModernAlertService.Initialize(mainTabbedPage);
        }

        protected override void OnStart()
        {
            // Eğer MainPage zaten bir şeyse ve Login değilse initialize et
            if (MainPage is not LoginView)
            {
                ModernAlertService.Initialize(MainPage);
            }
        }
    }
}
