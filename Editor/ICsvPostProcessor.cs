#nullable enable

namespace MasterDataDownloader
{
    public interface ICsvPostProcessor
    {
        string TargetPath { get; }
        string DisplayName { get; }
        void Execute(string csvPath);
    }
}
