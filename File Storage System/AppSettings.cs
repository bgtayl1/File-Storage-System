// AppSettings.cs
// Handles saving and loading application settings, project cache, and stats cache.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace FileFlow
{
    public static class AppSettings
    {
        private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string AppFolder = Path.Combine(AppDataPath, "FileFlowMFG");
        private static readonly string SettingsFile = Path.Combine(AppFolder, "settings.txt");
        private static readonly string ProjectCacheFile = Path.Combine(AppFolder, "projects.json");
        private static readonly string StatsCacheFile = Path.Combine(AppFolder, "stats.json");

        public static void SaveLastPath(string path)
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                File.WriteAllText(SettingsFile, path);
            }
            catch (Exception) { /* Silently fail */ }
        }

        public static string LoadLastPath()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    return File.ReadAllText(SettingsFile);
                }
            }
            catch (Exception) { /* Silently fail */ }
            return @"\\LPMSRV\Group Files"; // Default path
        }

        public static void SaveProjectsCache(ObservableCollection<Project> projects)
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                var json = JsonSerializer.Serialize(projects);
                File.WriteAllText(ProjectCacheFile, json);
            }
            catch (Exception) { /* Silently fail */ }
        }

        public static List<Project> LoadProjectsCache()
        {
            try
            {
                if (File.Exists(ProjectCacheFile))
                {
                    var json = File.ReadAllText(ProjectCacheFile);
                    return JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
                }
            }
            catch (Exception) { /* Silently fail */ }
            return new List<Project>();
        }

        public static void SaveStatsCache(Dictionary<string, (int, long, double)> cache)
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                var json = JsonSerializer.Serialize(cache);
                File.WriteAllText(StatsCacheFile, json);
            }
            catch (Exception) { /* Silently fail */ }
        }

        public static Dictionary<string, (int, long, double)> LoadStatsCache()
        {
            try
            {
                if (File.Exists(StatsCacheFile))
                {
                    var json = File.ReadAllText(StatsCacheFile);
                    return JsonSerializer.Deserialize<Dictionary<string, (int, long, double)>>(json) ?? new Dictionary<string, (int, long, double)>();
                }
            }
            catch (Exception) { /* Silently fail */ }
            return new Dictionary<string, (int, long, double)>();
        }
    }
}


