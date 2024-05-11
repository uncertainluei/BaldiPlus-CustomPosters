using BepInEx;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Compression;

namespace LuisRandomness.BBPCustomPosters.Packs
{
    public static class PackFormatReader
    {
        private static Dictionary<PluginInfo, List<Action<string, string>>> readChecks = new Dictionary<PluginInfo, List<Action<string, string>>>();
        public static void AddReadCheck(PluginInfo plugin, Action<string, string> action)
        {
            if (plugin == null)
                throw new NullReferenceException("'plugin' is null!");
            if (action == null)
                throw new NullReferenceException("'action' is null!");

            List<Action<string, string>> actions;

            if (!readChecks.TryGetValue(plugin, out actions))
            {
                actions = new List<Action<string, string>>();
                readChecks.Add(plugin, actions);
            }

            actions.Add(action);
        }

        public static bool TryGrabFormat(string path, out PackFormat output)
        {
            return TryGrabFormat(path, Path.GetExtension(path), out output);
        }

        public static bool TryGrabFormat(string path, string extension, out PackFormat output)
        {
            output = null;
            outputFormat = null;

            foreach (List<Action<string, string>> actions in readChecks.Values)
                foreach (Action<string,string> action in actions)
                {
                    action.Invoke(path, extension);
                    if (outputFormat != null)
                    {
                        output = outputFormat;
                        return true;
                    }
                }

            return false;
        }

        private static PackFormat outputFormat;

        public static PackFormat Result
        {
            set
            {
                if (outputFormat == null)
                    outputFormat = value;
            }
        }
    }

    public abstract class PackFormat
    {
        protected PackFileEntry[] entries;

        public PackFileEntry[] GetAllEntries()
        {
            if (entries == null)
                entries = GrabEntries();

            return entries;
        }

        public virtual void Reload()
        {
            entries = null;
        }

        protected abstract PackFileEntry[] GrabEntries();
        public abstract PackFileEntry Get(string path);
    }
    
    public abstract class PackFileEntry
    {
        public abstract string Name { get; }
        public abstract string FullName { get; }

        public abstract byte[] ReadAllBytes();
        public abstract string ReadAllText();
    }

    public class LocalPackFormat : PackFormat
    {
        public LocalPackFormat(string path)
        {
            dirPath = path;
        }

        private string dirPath;

        private List<PackFileEntry> _entries;

        protected override PackFileEntry[] GrabEntries()
        {
            if (_entries == null)
                _entries = new List<PackFileEntry>();

            AddEntriesFromDir(dirPath,"");

            entries = _entries.ToArray();
            _entries = null;
            return entries;
        }

        private void AddEntriesFromDir(string dir, string prefix)
        {
            foreach (string subDir in Directory.GetDirectories(dir))
                AddEntriesFromDir(subDir, prefix + Path.GetFileNameWithoutExtension(subDir) + "/");

            foreach (string file in Directory.GetFiles(dir))
                _entries?.Add(new LocalFileEntry(file, prefix+Path.GetFileName(file)));
        }

        public override PackFileEntry Get(string path)
        {
            string fullPath = Path.Combine(dirPath, path);

            if (!File.Exists(fullPath))
                return null;

            return new LocalFileEntry(fullPath, path);
        }
    }

    public class LocalFileEntry : PackFileEntry
    {
        public LocalFileEntry(string fullPath, string path)
        {
            filePath = fullPath;
            fullName = path;

            name = Path.GetFileNameWithoutExtension(fullName) + Path.GetExtension(fullName);
        }

        private string filePath;
        private string name;
        private string fullName;

        public override string Name => name;

        public override string FullName => fullName;

        public override string ReadAllText()
        {
            return File.ReadAllText(filePath);
        }

        public override byte[] ReadAllBytes()
        {
            return File.ReadAllBytes(filePath);
        }
    }

    public class ZipPackFormat : PackFormat
    {
        public ZipPackFormat(string path, ZipArchive archive)
        {
            zipPath = path;
            zipArchive = archive;
        }

        private string zipPath;
        private ZipArchive zipArchive;

        private List<PackFileEntry> _entries;
        
        protected override PackFileEntry[] GrabEntries()
        {
            if (_entries == null)
                _entries = new List<PackFileEntry>();

            foreach (ZipArchiveEntry entry in zipArchive.Entries)
                if (entry.Name != "") // Skip entries with empty names, as they're likely directories
                    _entries.Add(new ZipFileEntry(entry));

            entries = _entries.ToArray();
            _entries = null;
            return entries;
        }

        public override void Reload()
        {
            if (zipArchive != null)
                zipArchive.Dispose();

            zipArchive = ZipFile.OpenRead(zipPath);
            base.Reload();
        }

        public override PackFileEntry Get(string path)
        {
            ZipArchiveEntry entry = zipArchive.GetEntry(path);
            return entry == null ? null : new ZipFileEntry(entry);
        }
    }

    public class ZipFileEntry : PackFileEntry
    {
        public ZipFileEntry(ZipArchiveEntry archiveEntry)
        {
            entry = archiveEntry;

            fullName = archiveEntry.FullName;
            name = archiveEntry.Name;
        }

        ZipArchiveEntry entry;
        
        private string name;
        private string fullName;

        public override string Name => name;

        public override string FullName => fullName;

        public override string ReadAllText()
        {
            return entry.ReadAllText();
        }

        public override byte[] ReadAllBytes()
        {
            return entry.ReadAllBytes();
        }
    }
}
