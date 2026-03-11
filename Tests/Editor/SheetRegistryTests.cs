#nullable enable

using NUnit.Framework;
using UnityEngine;

namespace MasterDataDownloader.Tests
{
    [TestFixture]
    public sealed class SheetRegistryTests
    {
        [Test]
        public void Entries_NewRegistry_IsEmpty()
        {
            var registry = ScriptableObject.CreateInstance<SheetRegistry>();

            try
            {
                Assert.That(registry.Entries, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(registry);
            }
        }
    }
}
