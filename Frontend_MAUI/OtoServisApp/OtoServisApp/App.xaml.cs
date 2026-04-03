using Plugin.Firebase.CloudMessaging;
using System.Text;

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
            var navPage = new NavigationPage(new Views.LoginView());

            // Üst barın arka plan rengini turkuaz (Primary), yazı rengini beyaz yapıyoruz
            navPage.BarBackgroundColor = Color.FromArgb("#00BCD4");
            navPage.BarTextColor = Colors.White;
            MainPage = navPage;

            // UYGULAMA AÇIKKEN GELEN BİLDİRİMLERİ EKRANDA GÖSTERME KODU
            CrossFirebaseCloudMessaging.Current.NotificationReceived += (sender, args) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Uygulama açıkken bildirim gelirse ekrana pop-up olarak basar
                    await Current.MainPage.DisplayAlert(
                        args.Notification.Title,
                        args.Notification.Body,
                        "Tamam");
                });
            };
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
                await _httpClient.PostAsync("https://runny-scrutinizingly-ela.ngrok-free.dev/api/log-client-error", content);
            }
            catch { /* Sessiz geç */ }

            // Ayrıca cihazdaki crash.log dosyasına da yaz
            try
            {
                var logPath = Path.Combine(FileSystem.AppDataDirectory, "KapidanBakim_crash.log");
                var logText = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {ex?.Message}\n{ex?.StackTrace}\n\n";
                File.AppendAllText(logPath, logText);
            }
            catch { }
        }
    }
}
