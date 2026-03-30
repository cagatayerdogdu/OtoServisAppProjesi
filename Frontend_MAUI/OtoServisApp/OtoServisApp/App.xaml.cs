using Plugin.Firebase.CloudMessaging;

namespace OtoServisApp
{
    public partial class App : Application
    {
        public App()
        {
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
    }
}
