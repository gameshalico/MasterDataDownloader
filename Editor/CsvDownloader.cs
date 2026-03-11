#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MasterDataDownloader
{
    public static class CsvDownloader
    {
        public static async UniTask DownloadAsync(
            SheetEntry entry,
            CancellationToken ct = default)
        {
            var url = entry.BuildDownloadUrl();

            using var request = UnityWebRequest.Get(url);
            await request.SendWebRequest().WithCancellation(ct);

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(
                    $"HTTP error: {request.responseCode} {request.error} (URL: {url})");

            var directory = Path.GetDirectoryName(entry.OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(
                entry.OutputPath,
                request.downloadHandler.text,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                ct);

            AssetDatabase.ImportAsset(entry.OutputPath);
        }

        public static async UniTask DownloadAllAsync(
            SheetRegistry registry,
            IProgress<(int Current, int Total, string SheetName)>? progress = null,
            CancellationToken ct = default)
        {
            var entries = registry.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var entry = entries[i];
                progress?.Report((i + 1, entries.Count, entry.SheetName));
                await DownloadAsync(entry, ct);
            }
        }
    }
}
