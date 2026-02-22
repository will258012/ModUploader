using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.ObjectModel;

namespace ModUploader.Pages;

public sealed partial class ItemSelect : Page
{
    private ObservableCollection<ItemInfo> AllItems { get; } = new();
    public ObservableCollection<ItemInfo> FilteredItems { get; } = new();

    public ItemSelect()
    {
        App.Logger.Info($"ItemSelect entered");
        InitializeComponent();
        ListView.ItemsSource = FilteredItems;
        Loaded += PageSelectMod_Loaded;
    }

    private async void PageSelectMod_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadModsAsync();
    }

    private async Task LoadModsAsync()
    {
        AllItems.Clear();
        FilteredItems.Clear();
        try
        {
            LoadingRing.Visibility = SearchBox.Visibility = Visibility.Visible;
            ListViewText.Visibility = BtnRetry.Visibility = Visibility.Collapsed;
            App.Logger.Info($"Loading mod list...");
            var items = await UploadHelper.GetListAsync();

            foreach (var item in items)
            {
                AllItems.Add(new ItemInfo(item));
                FilteredItems.Add(new ItemInfo(item));
            }

            if (AllItems.Count == 0)
            {
                ListViewText.Visibility = BtnRetry.Visibility = Visibility.Visible;
                ListViewText.Text = Select_NoItems;
                SearchBox.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception e)
        {
            App.Logger.Error(e);
            ListViewText.Visibility = BtnRetry.Visibility = Visibility.Visible;
            ListViewText.Text = ModInfo_QueryFail + e.ToString();
            SearchBox.Visibility = Visibility.Collapsed;
        }
        finally
        {
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private void ModListView_ItemClick(
        object sender,
        ItemClickEventArgs e)
    {
        if (e.ClickedItem is ItemInfo mod)
        {
            Frame.Navigate(typeof(ItemEdit), mod, new DrillInNavigationTransitionInfo());
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string filter = SearchBox.Text.Trim().ToLower();
        FilteredItems.Clear();

        var filtered = string.IsNullOrWhiteSpace(filter)
            ? AllItems
            : AllItems.Where(m =>
                m.Name.ToLower().Contains(filter) ||
                m.PublishedFileId.ToString().Contains(filter));

        foreach (var mod in filtered)
            FilteredItems.Add(mod);

        if (FilteredItems.Count == 0)
        {
            ListViewText.Visibility = Visibility.Visible;
            ListViewText.Text = Select_NoResults;
        }
        else
        {
            ListViewText.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnRetry_Click(object sender, RoutedEventArgs e)
    {
        await LoadModsAsync();
    }
}