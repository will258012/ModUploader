using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ModUploader.Pages;

public sealed partial class Splash : Page
{
    private Func<Task> action;
    public Splash()
    {
        App.Logger.Info("Splash entered");
        InitializeComponent();
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        action = e.Parameter as Func<Task> ?? throw new ArgumentNullException();
        RunAction();
    }
    private async void RunAction()
    {
        try
        {
            ProgressRing.Visibility = Visibility.Visible;
            ErrorTextBlock.Visibility = BtnRetry.Visibility = Visibility.Collapsed;

            await action.Invoke();
        }
        catch (Exception e)
        {
            App.Logger.Error(e);
            ProgressRing.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Visibility = BtnRetry.Visibility =  Visibility.Visible;
            ErrorTextBlock.Text = Main_InitFail + e.Message;
        }
    }

    private void BtnRetry_Click(object sender, RoutedEventArgs e)
    {
        RunAction();
    }
}
