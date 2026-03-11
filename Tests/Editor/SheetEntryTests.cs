#nullable enable

using System;
using NUnit.Framework;

namespace MasterDataDownloader.Tests
{
    [TestFixture]
    public sealed class SheetEntryTests
    {
        [Test]
        public void BuildDownloadUrl_ValidEntry_ReturnsCorrectUrl()
        {
            var entry = new SheetEntry("abc123", "Sheet1");

            var url = entry.BuildDownloadUrl();

            Assert.That(url,
                Is.EqualTo("https://docs.google.com/spreadsheets/d/abc123/gviz/tq?tqx=out:csv&sheet=Sheet1"));
        }

        [Test]
        public void BuildDownloadUrl_SheetNameWithSpaces_IsUrlEncoded()
        {
            var entry = new SheetEntry("abc123", "My Sheet");

            var url = entry.BuildDownloadUrl();

            Assert.That(url, Does.Contain("sheet=My%20Sheet"));
        }

        [Test]
        public void BuildDownloadUrl_SheetNameWithJapanese_IsUrlEncoded()
        {
            var entry = new SheetEntry("abc123", "マスタ");

            var url = entry.BuildDownloadUrl();

            Assert.That(url, Does.Not.Contain("sheet=マスタ"));
            Assert.That(url, Does.StartWith("https://docs.google.com/spreadsheets/d/abc123/gviz/tq?tqx=out:csv&sheet="));
        }

        [Test]
        public void BuildDownloadUrl_EmptySheetId_ThrowsInvalidOperationException()
        {
            var entry = new SheetEntry("", "Sheet1");

            Assert.Throws<InvalidOperationException>(() => entry.BuildDownloadUrl());
        }

        [Test]
        public void BuildDownloadUrl_NullSheetId_ThrowsInvalidOperationException()
        {
            var entry = new SheetEntry(null!, "Sheet1");

            Assert.Throws<InvalidOperationException>(() => entry.BuildDownloadUrl());
        }

        [Test]
        public void BuildDownloadUrl_EmptySheetName_ThrowsInvalidOperationException()
        {
            var entry = new SheetEntry("abc123", "");

            Assert.Throws<InvalidOperationException>(() => entry.BuildDownloadUrl());
        }

        [Test]
        public void BuildDownloadUrl_NullSheetName_ThrowsInvalidOperationException()
        {
            var entry = new SheetEntry("abc123", null!);

            Assert.Throws<InvalidOperationException>(() => entry.BuildDownloadUrl());
        }

        [Test]
        public void Properties_ReturnCorrectValues()
        {
            var entry = new SheetEntry("id1", "name1", "Assets/output.csv");

            Assert.That(entry.SheetId, Is.EqualTo("id1"));
            Assert.That(entry.SheetName, Is.EqualTo("name1"));
            Assert.That(entry.OutputPath, Is.EqualTo("Assets/output.csv"));
        }
    }
}
