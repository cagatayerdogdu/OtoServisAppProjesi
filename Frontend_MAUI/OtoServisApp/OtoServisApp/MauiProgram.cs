using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.ApplicationModel;
using OtoServisApp.Services;

#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
using Plugin.Firebase.Core;
#endif

namespace OtoServisApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("FontAwesomeSolid.otf", "FASolid");
                fonts.AddFont("fa-brands-400.ttf", "FABrands");

                // Geçici çözüm: Font ailesini doğrudan dosya adıyla da dene
                //fonts.AddFont("FontAwesomeSolid.otf", "FontAwesomeSolid");
            });

        // Firebase başlatma
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, state) =>
            {
                CrossFirebase.Initialize(activity);
            }));
#elif IOS
            events.AddiOS(ios => ios.FinishedLaunching((app, launchOptions) =>
            {
                // iOS'ta Firebase otomatik olarak başlatılır, ekstra kod gerekmez.
                return true;
            }));
#endif
        });

        // Servis kayıtları
        builder.Services.AddSingleton<IBadge>(Badge.Default);
        builder.Services.AddSingleton<NotificationBadgeService>();
        builder.Services.AddSingleton<ApiService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}