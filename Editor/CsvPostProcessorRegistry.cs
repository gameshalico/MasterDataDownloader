#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MasterDataDownloader
{
    public static class CsvPostProcessorRegistry
    {
        private static Dictionary<string, List<ICsvPostProcessor>>? _cache;

        public static IReadOnlyList<ICsvPostProcessor> GetAll()
        {
            EnsureCache();
            return _cache!.Values.SelectMany(v => v).ToList();
        }

        public static IReadOnlyList<ICsvPostProcessor> GetForPath(string outputPath)
        {
            EnsureCache();
            return _cache!.TryGetValue(outputPath, out var list)
                ? list
                : Array.Empty<ICsvPostProcessor>();
        }

        public static int ExecuteForPath(string outputPath)
        {
            var processors = GetForPath(outputPath);
            var failCount = 0;
            foreach (var processor in processors)
            {
                try
                {
                    processor.Execute(outputPath);
                    Debug.Log($"PostProcess: {processor.DisplayName} for {outputPath}");
                }
                catch (Exception e)
                {
                    failCount++;
                    Debug.LogError($"PostProcess failed: {processor.DisplayName} for {outputPath}");
                    Debug.LogException(e);
                }
            }
            return failCount;
        }

        public static void ClearCache()
        {
            _cache = null;
        }

        private static void EnsureCache()
        {
            if (_cache != null) return;

            _cache = new Dictionary<string, List<ICsvPostProcessor>>();

            var types = TypeCache.GetTypesDerivedFrom<ICsvPostProcessor>();
            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    continue;

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogWarning(
                        $"[MasterDataDownloader] {type.FullName} has no parameterless constructor. Skipped.");
                    continue;
                }

                try
                {
                    var instance = (ICsvPostProcessor)Activator.CreateInstance(type)!;
                    if (!_cache.TryGetValue(instance.TargetPath, out var list))
                    {
                        list = new List<ICsvPostProcessor>();
                        _cache[instance.TargetPath] = list;
                    }
                    list.Add(instance);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[MasterDataDownloader] Failed to instantiate {type.FullName}: {e.Message}");
                }
            }
        }
    }
}
