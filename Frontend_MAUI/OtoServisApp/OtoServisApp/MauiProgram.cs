using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.ApplicationModel;
using OtoServisApp.Services;
using OtoServisApp.Views;

#if IOS
using UIKit;
#endif

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
            });

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, state) =>
            {
                CrossFirebase.Initialize(activity);
                if (activity.Intent?.Extras != null)
                {
                    var keySet = activity.Intent.Extras.KeySet();
                    if (keySet != null && keySet.Contains("google.message_id"))
                        App.BekleyenBildirimVarMi = true;
                }
            }));
            events.AddAndroid(android => android.OnNewIntent((activity, intent) =>
            {
                if (intent?.Extras != null)
                {
                    var keySet = intent.Extras.KeySet();
                    if (keySet != null && keySet.Contains("google.message_id"))
                    {
                        App.BekleyenBildirimVarMi = true;
                        if (Application.Current?.MainPage != null)
                        {
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                if (Application.Current.MainPage is NavigationPage nav)
                                    await nav.Navigation.PushAsync(new NotificationsView());
                                else if (Application.Current.MainPage is TabbedPage tab)
                                {
                                    var cn = tab.CurrentPage as NavigationPage;
                                    if (cn != null) await cn.Navigation.PushAsync(new NotificationsView());
                                }
                            });
                        }
                    }
                }
            }));
#elif IOS
            events.AddiOS(ios => ios.FinishedLaunching((app, launchOptions) =>
            {
                if (launchOptions?.ContainsKey(UIApplication.LaunchOptionsRemoteNotificationKey) == true)
                    App.BekleyenBildirimVarMi = true;
                return true;
            }));
#endif
        });

        builder.Services.AddSingleton<IBadge>(Badge.Default);
        builder.Services.AddSingleton<NotificationBadgeService>();
        builder.Services.AddSingleton<ApiService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}