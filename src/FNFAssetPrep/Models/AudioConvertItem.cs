using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FNFAssetPrep.Models;

public sealed class AudioConvertItem : INotifyPropertyChanged
{
    private string _status = "Ready";
    private string? _outputPath;

    public required string SourcePath { get; init; }
    public string FileName => Path.GetFileName(SourcePath);
    public string SourceFormat => Path.GetExtension(SourcePath).TrimStart('.').ToUpperInvariant();

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

    public string? OutputPath
    {
        get => _outputPath;
        set
        {
            if (_outputPath == value) return;
            _outputPath = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
