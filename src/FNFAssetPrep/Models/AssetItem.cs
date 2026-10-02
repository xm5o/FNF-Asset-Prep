using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace FNFAssetPrep.Models;

public sealed class AssetItem : INotifyPropertyChanged
{
    private string _status = "Ready";
    private string? _statusDetail;

    public required string SourcePath { get; init; }
    public string FileName => Path.GetFileName(SourcePath);
    public string SourceFormat => Path.GetExtension(SourcePath).TrimStart('.').ToUpperInvariant();
    public required string Type { get; init; }
    public required string TargetFormat { get; init; }

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public string? StatusDetail
    {
        get => _statusDetail;
        set
        {
            if (_statusDetail == value) return;
            _statusDetail = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
