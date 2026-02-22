using Microsoft.UI.Xaml.Controls;

namespace ModUploader.Pages;

public sealed partial class Home : Page
{
    public Home()
    {
        App.Logger.Info($"Home entered");
        InitializeComponent();
        TextBlock.Text = string.Format(Home_Welcome, SteamClient.Name);
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ItemEdit));
    }

    private void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ItemSelect));
    }
}
