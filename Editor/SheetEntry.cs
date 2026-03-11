#nullable enable

using System;
using UnityEngine;
using UnityEngine.Networking;

namespace MasterDataDownloader
{
    [Serializable]
    public sealed class SheetEntry
    {
        [SerializeField] private string _sheetId = "";
        [SerializeField] private string _sheetName = "";
        [SerializeField] private string _outputPath = "";

        public string SheetId => _sheetId;
        public string SheetName => _sheetName;
        public string OutputPath => _outputPath;

        public string BuildDownloadUrl()
        {
            if (string.IsNullOrEmpty(_sheetId))
                throw new InvalidOperationException("SheetId is empty.");
            if (string.IsNullOrEmpty(_sheetName))
                throw new InvalidOperationException("SheetName is empty.");

            var encodedSheetName = UnityWebRequest.EscapeURL(_sheetName);
            return $"https://docs.google.com/spreadsheets/d/{_sheetId}/gviz/tq?tqx=out:csv&sheet={encodedSheetName}";
        }
    }
}
