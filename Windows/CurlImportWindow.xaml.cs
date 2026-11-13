using FFmpegDownloader.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;

namespace FFmpegDownloader.Windows;

public sealed partial class CurlImportWindow : FluentWindow
{
    private readonly CurlImportViewModel curlImportViewModel;

    public string CommandText => curlImportViewModel.CommandText;

    public CurlImportWindow(CurlImportViewModel curlImportViewModel)
    {
        InitializeComponent();

        DataContext = curlImportViewModel;

        this.curlImportViewModel = curlImportViewModel;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CommandTextBox.Text))
            return;

        curlImportViewModel.CommandText = CommandTextBox.Text;
        DialogResult = true;
    }
}
