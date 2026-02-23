using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ModUploader.Pages;

public sealed partial class ItemEdit : Page
{
    private ItemInfo? editingItem = null;
    private string? TxtPreviewPath;
    private bool _hasConfirmedNavigation;
    public ObservableCollection<string> CustomTags { get; set; } = [];
    public ItemEdit()
    {
        App.Logger.Info($"ItemEdit entered");
        InitializeComponent();
    }
    #region Page overrides
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ItemInfo item)
        {
            editingItem = item;
            TxtName.Text = item.Name;
            TxtDescription.Visibility = Patches.QueryLanguagePatch.IsDescriptionEditionDisabled ? Visibility.Collapsed : Visibility.Visible;
            TxtDescription.Text = Patches.QueryLanguagePatch.IsDescriptionEditionDisabled ? "" : item.Description;
            UpdatePreviewOnlyCheckbox.Visibility = Visibility.Visible;
            TypeChoiceExpander.IsExpanded = false;
            PreviewImage.Source = new BitmapImage(new Uri(editingItem.PreviewImageUrl));
            ParseTagsIntoTypeChoice(item.Tags);
        }
        else
        {
            PreviewImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/PreviewImage.png"));
        }

        App.appWindow.Closing += AppWindow_Closing;
        _hasConfirmedNavigation = false;
    }

    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (_hasConfirmedNavigation)
        {
            App.appWindow.Closing -= AppWindow_Closing;
            return;
        }

        e.Cancel = true;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Edit_ConfirmCancel,
            PrimaryButtonText = Edit_Confirm,
            CloseButtonText = Cancel,
            DefaultButton = ContentDialogButton.None
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            _hasConfirmedNavigation = true;
            e.Cancel = false;
            Frame.GoBack();
        }
    }


    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        try
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = Edit_ConfirmCancel,
                PrimaryButtonText = Edit_Confirm,
                CloseButtonText = Cancel,
                DefaultButton = ContentDialogButton.None
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                args.Cancel = false;
                Application.Current.Exit();
            }
        }
        catch
        {
            Application.Current.Exit();
        }
    }

    #endregion

    #region Left Zone

    private async void PreviewImage_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add(".png");

        StorageFile file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            TxtPreviewPath = file.Path;

            PreviewImage.Source =
                new BitmapImage(new Uri(file.Path));

            RemoveRedBrush(PreviewImageBorder);
        }
    }

    private void GeneralRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GeneralRadioButtons.SelectedItem != null)
        {
            AssetsListView.SelectedItems.Clear();
            MiscRadioButtons.SelectedIndex = -1;
            if (GeneralRadioButtons.SelectedItem is RadioButton { Content: "Mod" })
            {
                for (int i = CustomTags.Count - 1; i >= 0; i--)
                {
                    if (UploadHelper.CompatibleRegex.IsMatch(CustomTags[i]))
                        CustomTags.RemoveAt(i);
                }

                CustomTags.Add(UploadHelper.Instance.CompatibleTag);
            }
            else CustomTags.Remove(UploadHelper.Instance.CompatibleTag);
        }
    }

    private void MiscRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MiscRadioButtons.SelectedItem != null)
        {
            AssetsListView.SelectedItems.Clear();
            GeneralRadioButtons.SelectedIndex = -1;
            CustomTags.Remove(UploadHelper.Instance.CompatibleTag);
        }
    }

    private void AssetsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AssetsListView.SelectedItems.Count > 0)
        {
            GeneralRadioButtons.SelectedIndex = -1;
            MiscRadioButtons.SelectedIndex = -1;
            CustomTags.Remove(UploadHelper.Instance.CompatibleTag);
        }
    }

    private void CustomTagsBox_TokenItemAdding(CommunityToolkit.WinUI.Controls.TokenizingTextBox sender, CommunityToolkit.WinUI.Controls.TokenItemAddingEventArgs args)
    {
        string text = args.TokenText?.Trim() ?? string.Empty;
        text = text.Replace(" ", "");

        if (string.IsNullOrWhiteSpace(text) || CustomTags.Contains(text))
        {
            args.Cancel = true;
            return;
        }

        args.Item = text;
    }
    private void ParseTagsIntoTypeChoice(string[] tags)
    {
        if (tags == null || tags.Length == 0)
            return;

        GeneralRadioButtons.SelectedItem = null;
        MiscRadioButtons.SelectedItem = null;
        AssetsListView.SelectedItems.Clear();

        foreach (var tag in tags)
        {
            var trimmed = tag.Trim().ToLower();

            foreach (RadioButton item in GeneralRadioButtons.Items)
            {
                if (item.Content?.ToString()?.Trim().ToLower() == trimmed)
                {
                    GeneralRadioButtons.SelectedItem = item;
                    goto NextTag;
                }
            }

            foreach (RadioButton item in MiscRadioButtons.Items)
            {
                if (item.Content?.ToString()?.Trim().ToLower() == trimmed)
                {
                    MiscRadioButtons.SelectedItem = item;
                    goto NextTag;
                }
            }

            foreach (ListViewItem item in AssetsListView.Items)
            {
                if (item.Content?.ToString()?.Trim().ToLower() == trimmed)
                {
                    AssetsListView.SelectedItems.Add(item);
                    goto NextTag;
                }
            }

            CustomTags.Add(trimmed);

        NextTag:
            continue;
        }
    }
    private string[] GetSelectedTags()
    {
        var general = (GeneralRadioButtons.SelectedItem as RadioButton)?.Content?.ToString();

        var misc = (MiscRadioButtons.SelectedItem as RadioButton)?.Content?.ToString();

        var assets = AssetsListView.SelectedItems
                    .Cast<ListViewItem>()
                    .Select(x => x.Content.ToString())
                    .ToList();

        string[] tags;

        if (!string.IsNullOrEmpty(general))
        {
            tags = [general];
        }
        else if (!string.IsNullOrEmpty(misc))
        {
            tags = [misc];
        }
        else if (assets.Count > 0)
        {
            tags = [.. assets];
        }
        else tags = Array.Empty<string>();

        tags = tags.Union(
            CustomTags
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(t => !string.IsNullOrWhiteSpace(t)))
            .ToArray();

        return tags;
    }

    #endregion

    #region Right Zone
    private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            TxtNameError.Visibility = Visibility.Visible;
            AttachRedBrush(TxtName);
        }
        else
        {
            TxtNameError.Visibility = Visibility.Collapsed;
            RemoveRedBrush(TxtName);
        }
    }
    private void TxtContentFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtContentFolder.Text) && UpdatePreviewOnlyCheckbox.IsChecked == false)
        {
            TxtContentFolderError.Visibility = Visibility.Visible;
            AttachRedBrush(TxtContentFolder);
        }
        else
        {
            TxtContentFolderError.Visibility = Visibility.Collapsed;
            RemoveRedBrush(TxtContentFolder);
        }
    }

    private async void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add("*");

        StorageFolder folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            TxtContentFolder.Text = folder.Path;
        }
    }

    private void UpdatePreviewOnlyCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        ValidateForm();
        editingItem?.UpdatePreviewOnly = true;

        if (TxtPreviewPath == null)
        {
            AttachRedBrush(PreviewImageBorder);
        }
        else
        {
            RemoveRedBrush(PreviewImageBorder);
        }

        TxtName.IsEnabled = TxtChangeLog.IsEnabled = TxtContentFolder.IsEnabled = TxtDescription.IsEnabled = TypeChoiceExpander.IsEnabled = false;
    }

    private void UpdatePreviewOnlyCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        ValidateForm();
        RemoveRedBrush(PreviewImageBorder);
        editingItem?.UpdatePreviewOnly = false;
        TxtName.IsEnabled = TxtChangeLog.IsEnabled = TxtContentFolder.IsEnabled = TxtDescription.IsEnabled = TypeChoiceExpander.IsEnabled = true;
    }
    #endregion

    #region Validation
    private bool ValidateForm()
    {
        TxtName_TextChanged(null, null);
        TxtContentFolder_TextChanged(null, null);

        if (GeneralRadioButtons.SelectedItem == null && MiscRadioButtons.SelectedItem == null && AssetsListView.SelectedItems.Count == 0)
        {
            TypeChoiceExpanderError.Visibility = Visibility.Visible;
            AttachRedBrush(TypeChoiceExpander);
        }
        else
        {
            TypeChoiceExpanderError.Visibility = Visibility.Collapsed;
            RemoveRedBrush(TypeChoiceExpander);
        }

        if (editingItem?.UpdatePreviewOnly == true && TxtPreviewPath == null)
        {
            AttachRedBrush(PreviewImageBorder);
            return false;
        }
        else
        {
            RemoveRedBrush(PreviewImageBorder);
        }

        return TxtNameError.Visibility == Visibility.Collapsed &&
            TxtContentFolderError.Visibility == Visibility.Collapsed &&
            TypeChoiceExpanderError.Visibility == Visibility.Collapsed;
    }

    private void AttachRedBrush<T>(T element) where T : Control
    {

        element.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
        element.BorderThickness = new Thickness(2);
    }

    private void AttachRedBrush(Border border)
    {
        border.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
        border.BorderThickness = new Thickness(2);
    }
    private void RemoveRedBrush<T>(T element) where T : Control
    {
        element.ClearValue(BorderBrushProperty);
        element.ClearValue(BorderThicknessProperty);
    }
    private void RemoveRedBrush(Border element)
    {
        element.BorderThickness = new Thickness(0);
        element.ClearValue(BorderBrushProperty);
        element.ClearValue(BorderThicknessProperty);
        element.Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
    }
    private void BtnUpload_Click(object sender, RoutedEventArgs e)
    {
        BtnUpload.Flyout?.Hide();
        if (!ValidateForm()) return;

        if (editingItem == null)
        {
            editingItem = new ItemInfo(
                TxtName.Text,
                string.IsNullOrEmpty(TxtDescription.Text) ? null : TxtDescription.Text,
                GetSelectedTags());
        }
        else
        {
            editingItem.Name = TxtName.Text;
            editingItem.Description = string.IsNullOrEmpty(TxtDescription.Text) ? null : TxtDescription.Text;
            editingItem.Tags = GetSelectedTags();
        }

        editingItem.ChangeLog = string.IsNullOrEmpty(TxtChangeLog.Text) ? null : TxtChangeLog.Text;
        editingItem.ContentFolderPath = TxtContentFolder.Text;
        editingItem.PreviewImagePath = TxtPreviewPath;

        _hasConfirmedNavigation = true;

        Frame.Navigate(typeof(ItemUpload), editingItem, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
    }
    #endregion
}
