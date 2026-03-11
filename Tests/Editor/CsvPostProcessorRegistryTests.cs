#nullable enable

using System.Linq;
using NUnit.Framework;

namespace MasterDataDownloader.Tests
{
    public sealed class TestPostProcessorA : ICsvPostProcessor
    {
        public string TargetPath => "Assets/Test/A.csv";
        public string DisplayName => "TestA";
        public bool Executed { get; private set; }

        public void Execute(string csvPath)
        {
            Executed = true;
        }
    }

    public sealed class TestPostProcessorB : ICsvPostProcessor
    {
        public string TargetPath => "Assets/Test/B.csv";
        public string DisplayName => "TestB";

        public void Execute(string csvPath)
        {
        }
    }

    [TestFixture]
    public sealed class CsvPostProcessorRegistryTests
    {
        [SetUp]
        public void SetUp()
        {
            CsvPostProcessorRegistry.ClearCache();
        }

        [Test]
        public void GetAll_ReturnsAllRegisteredProcessors()
        {
            var all = CsvPostProcessorRegistry.GetAll();

            Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(all.Any(p => p is TestPostProcessorA), Is.True);
            Assert.That(all.Any(p => p is TestPostProcessorB), Is.True);
        }

        [Test]
        public void GetForPath_ExistingPath_ReturnsMatchingProcessors()
        {
            var processors = CsvPostProcessorRegistry.GetForPath("Assets/Test/A.csv");

            Assert.That(processors.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(processors.Any(p => p is TestPostProcessorA), Is.True);
        }

        [Test]
        public void GetForPath_NonExistingPath_ReturnsEmpty()
        {
            var processors = CsvPostProcessorRegistry.GetForPath("Assets/NonExistent.csv");

            Assert.That(processors, Is.Empty);
        }

        [Test]
        public void ExecuteForPath_ExistingPath_ExecutesProcessor()
        {
            CsvPostProcessorRegistry.ExecuteForPath("Assets/Test/A.csv");

            var processors = CsvPostProcessorRegistry.GetForPath("Assets/Test/A.csv");
            var processor = processors.OfType<TestPostProcessorA>().First();
            Assert.That(processor.Executed, Is.True);
        }

        [Test]
        public void ExecuteForPath_NonExistingPath_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                CsvPostProcessorRegistry.ExecuteForPath("Assets/NonExistent.csv"));
        }

        [Test]
        public void ClearCache_ThenGetAll_RebuildsCache()
        {
            var first = CsvPostProcessorRegistry.GetAll();
            CsvPostProcessorRegistry.ClearCache();
            var second = CsvPostProcessorRegistry.GetAll();

            Assert.That(second.Count, Is.EqualTo(first.Count));
        }
    }
}
