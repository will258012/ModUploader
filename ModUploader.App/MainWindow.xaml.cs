using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using ModUploader.Pages;
using System.Reflection;

namespace ModUploader;
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {

        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString();

        var copyrightAttr = assembly
            .GetCustomAttribute<AssemblyCopyrightAttribute>()?
            .Copyright ?? "";

        WatermarkText.Text = $"{copyrightAttr} {version}";

        MainFrame.Navigate(typeof(Splash), (Func<Task>)OnSplash, new SuppressNavigationTransitionInfo());
    }
    internal void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (MainFrame.CanGoBack)
            MainFrame.GoBack();
    }
    private void MainFrame_Navigated(object sender, NavigationEventArgs e)
    {
        var currentPageType = e.SourcePageType;

        if (currentPageType == typeof(ItemSelect) ||
            currentPageType == typeof(ItemEdit))
        {
            BackButton.Visibility = Visibility.Visible;
        }
        else
        {
            BackButton.Visibility = Visibility.Collapsed;
        }
    }
    private async Task OnSplash()
    {
        if (!Utils.Ping("https://steamcommunity.com"))
            throw new HttpRequestException(Resources.Resource_EResult.k_EResultConnectFailed);

        if (!SteamClient.IsValid)
            SteamClient.Init(UploadHelper.CSL_APPID);

        App.LoadAssembly();
        App.ApplyHarmonyPatches();
        MainFrame.DispatcherQueue.TryEnqueue(() =>
        {
            MainFrame.Navigate(typeof(Home), null, new DrillInNavigationTransitionInfo());
        });
    }
}
