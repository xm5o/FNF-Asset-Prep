using System.Windows.Controls;
using FNFAssetPrep.Services;

namespace FNFAssetPrep.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
        Refresh();
    }

    public void Refresh()
    {
        RuntimeText.Text = ".NET " + Environment.Version;
        FfmpegText.Text = AssetProcessor.HasAudioEngine ? "Ready" : "Missing";
        YtDlpText.Text = MediaDownloadService.HasDownloader ? "Ready" : "Missing";
    }
}
