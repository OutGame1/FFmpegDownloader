using FFmpegDownloader.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;

namespace FFmpegDownloader.Windows;

public sealed partial class AdvancedOptionsWindow : FluentWindow
{
    public AdvancedOptionsWindow(AdvancedOptionsViewModel advancedViewModel)
    {
        InitializeComponent();

        DataContext = advancedViewModel;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
