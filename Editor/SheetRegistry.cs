#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace MasterDataDownloader
{
    [CreateAssetMenu(
        fileName = "SheetRegistry",
        menuName = "MasterDataDownloader/Sheet Registry")]
    public sealed class SheetRegistry : ScriptableObject
    {
        [SerializeField] private List<SheetEntry> _entries = new();

        public IReadOnlyList<SheetEntry> Entries => _entries;
    }
}
