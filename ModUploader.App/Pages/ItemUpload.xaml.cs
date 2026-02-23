using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Steamworks.Ugc;
using System.Diagnostics;

namespace ModUploader.Pages;

public sealed partial class ItemUpload : Page
{
    private ItemInfo mod;

    public ItemUpload()
    {
        App.Logger.Info($"ItemUpload entered");
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        mod = e.Parameter as ItemInfo ?? throw new ArgumentNullException();

        StartUpload();
    }

    private async void StartUpload()
    {
        try
        {
            App.Logger.Info("Starting upload");
            UploadProgressRing.IsActive = true;
            UploadProgressRing.Visibility = StatusText.Visibility = Visibility.Visible;
            ResultPanel.Visibility = Visibility.Collapsed;

            var progress = new Progress<float>(value =>
            {
                if (value == 0)
                {
                    UploadProgressRing.IsIndeterminate = true;
                }
                else
                {
                    UploadProgressRing.IsIndeterminate = false;
                    UploadProgressRing.Value = value * 100f;
                }
            });

            PublishResult result = await UploadHelper.Instance.StartUploadAsync(mod, progress);

            UploadProgressRing.IsActive = false;
            UploadProgressRing.Visibility = StatusText.Visibility = Visibility.Collapsed;

            ResultPanel.Visibility = Visibility.Visible;

            if (result.Success)
            {
                ResultIcon.Text = "\uE73E";
                ResultMessage.Text = Upload_Success;
                BtnRetry.Visibility = BtnOpenLog.Visibility = Visibility.Collapsed;
                Process.Start("explorer.exe", $"steam://url/CommunityFilePage/{result.FileId.Value}");
            }
            else
            {
                ResultIcon.Text = "\uEA39";
                ResultMessage.Text = $"{(mod.IsNewItem ? Main_CreateFail : Main_UpdateFail)} {result.Result.ToLocalizedString()} ({result.Result})";
                App.Logger.Error(ResultMessage.Text);
                BtnRetry.Visibility = BtnOpenLog.Visibility = Visibility.Visible;
            }
        }
        catch (Exception e)
        {
            App.Logger.Error(e);
            UploadProgressRing.IsActive = false;
            UploadProgressRing.Visibility = Visibility.Collapsed;
            StatusText.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;

            ResultIcon.Text = "\uEA39";
            ResultMessage.Text = (mod.IsNewItem ? Main_CreateFail : Main_UpdateFail) + Environment.NewLine + e;

            BtnRetry.Visibility = Visibility.Visible;
            BtnOpenLog.Visibility = Visibility.Visible;
        }
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (ResultIcon.Text == "\uEA39")
            Frame.GoBack();
        else
            Frame.Navigate(typeof(Home));
    }

    private void BtnOpenLog_Click(object sender, RoutedEventArgs e)
    {
        string targetPath = Path.Combine(AppContext.BaseDirectory, "logs", DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        App.Logger.Info($"Opening log file: {targetPath}");
        if (File.Exists(targetPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = true
            });
        }
    }

    private void BtnRetry_Click(object sender, RoutedEventArgs e)
    {
        StartUpload();
    }
}