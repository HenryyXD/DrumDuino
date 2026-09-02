using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DrumDuino.App.ViewModels;

namespace DrumDuino.App.Views;

public partial class MainWindow : Window
{
    private bool _closeRequested;

    public MainWindow()
    {
        InitializeComponent();
        ImportIniButton.Click += OnImportIniClick;
        LoadJsonButton.Click += OnLoadJsonClick;
        SaveJsonButton.Click += OnSaveJsonClick;
        Closing += OnClosing;
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext!;

    private async void OnImportIniClick(object? sender, RoutedEventArgs e)
    {
        var file = await PickFileAsync("Importar pins.ini", ["ini"]);
        if (file is not null)
        {
            ViewModel.ImportPinsIni(file.Path.LocalPath);
        }
    }

    private async void OnLoadJsonClick(object? sender, RoutedEventArgs e)
    {
        var file = await PickFileAsync("Carregar preset JSON", ["json"]);
        if (file is not null)
        {
            ViewModel.LoadJsonPreset(file.Path.LocalPath);
        }
    }

    private async void OnSaveJsonClick(object? sender, RoutedEventArgs e)
    {
        var file = await SaveFileAsync("Salvar preset JSON", "kit.json", ["json"]);
        if (file is not null)
        {
            ViewModel.SaveJsonPreset(file.Path.LocalPath);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeRequested)
        {
            return;
        }

        e.Cancel = true;
        _ = CloseGracefullyAsync();
    }

    private async Task CloseGracefullyAsync()
    {
        try
        {
            await ViewModel.DisposeAsync();
        }
        finally
        {
            _closeRequested = true;
            Close();
        }
    }

    private async Task<IStorageFile?> PickFileAsync(string title, IReadOnlyList<string> extensions)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(title)
                {
                    Patterns = extensions.Select(ext => $"*.{ext}").ToList()
                }
            ]
        });

        return files.Count > 0 ? files[0] : null;
    }

    private async Task<IStorageFile?> SaveFileAsync(string title, string defaultName, IReadOnlyList<string> extensions)
    {
        return await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultName,
            FileTypeChoices =
            [
                new FilePickerFileType(title)
                {
                    Patterns = extensions.Select(ext => $"*.{ext}").ToList()
                }
            ]
        });
    }
}
