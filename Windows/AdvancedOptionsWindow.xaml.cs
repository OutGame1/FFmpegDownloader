using FFmpegDownloader.ViewModels;
using Wpf.Ui.Controls;

namespace FFmpegDownloader.Windows;

public partial class AdvancedOptionsWindow : FluentWindow
{
    public AdvancedOptionsWindow(AdvancedOptionsViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
