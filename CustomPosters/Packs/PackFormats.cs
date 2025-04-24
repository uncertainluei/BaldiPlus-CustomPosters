using BepInEx;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.CustomPosters.Packs
{
    public static class PackFormatReader
    {
        public delegate PackFormat ReadCheck(string path, string ext);

        private static Dictionary<PluginInfo, List<ReadCheck>> readChecks = new Dictionary<PluginInfo, List<ReadCheck>>();
        public static void AddReadCheck(PluginInfo plugin, ReadCheck function)
        {
            if (plugin == null)
                throw new NullReferenceException("'plugin' is null!");
            if (function == null)
                throw new NullReferenceException("'function' is null!");

            if (!readChecks.TryGetValue(plugin, out List<ReadCheck> functions))
            {
                functions = new List<ReadCheck>();
                readChecks.Add(plugin, functions);
            }
            functions.Add(function);
        }

        public static bool TryGrabFormat(string path, out PackFormat output)
        {
            return TryGrabFormat(path, Path.GetExtension(path), out output);
        }

        public static bool TryGrabFormat(string path, string extension, out PackFormat output)
        {
            output = null;

            foreach (List<ReadCheck> actions in readChecks.Values)
                foreach (ReadCheck action in actions)
                {
                    output = action.Invoke(path, extension);
                    if (output != null)
                        return true;
                }

            return false;
        }

        internal static void InitReadChecks(PluginInfo plugin)
        {
            // Local file paths
            PackFormatReader.AddReadCheck(plugin, (string path, string ext) =>
            {
                if (!Directory.Exists(path))
                    return null;

                return new LocalPackFormat(path);
            });

            // .ZIP archives
            PackFormatReader.AddReadCheck(plugin, (string path, string ext) =>
            {
                if (ext != ".zip") return null;

                ZipArchive archive;
                try
                {
                    archive = ZipFile.OpenRead(path);
                }
                catch
                {
                    return null;
                }

                return new ZipPackFormat(path, archive);
            });
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

        private readonly string dirPath;

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

        public override void Reload()
        {
            _entries?.Clear();
            base.Reload();
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

        private readonly string filePath;
        private readonly string name;
        private readonly string fullName;

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

        ~ZipPackFormat()
        {
            zipArchive?.Dispose();
        }

        private readonly string zipPath;
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
            zipArchive?.Dispose();
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

        private readonly ZipArchiveEntry entry;
        private readonly string name;
        private readonly string fullName;

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

    public static class ZipExtensions
    {
        private static Texture2D errorPlaceholder;

        // This does not use the API equivalent as it requires a file input to be provided
        public static bool TryCreateTexture(this byte[] bytes, string name, out Texture2D outputTexture)
        {
            if (errorPlaceholder == null)
            {
                errorPlaceholder = new Texture2D(1, 1, TextureFormat.ARGB32, false)
                {
                    filterMode = FilterMode.Point,
                    name = "ErrorQuestionMark"
                };

                // Load an invalid texture
                errorPlaceholder.LoadImage(new byte[0]);
            }

            outputTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                name = Path.GetFileNameWithoutExtension(name)
            };

            if (!outputTexture.LoadImage(bytes) || outputTexture.GetPixels() == errorPlaceholder.GetPixels())
            {
                UnityEngine.Object.Destroy(outputTexture);
                return false;
            }
            return true;
        }

        public static byte[] ReadAllBytes(this ZipArchiveEntry entry)
        {
            using (Stream openedStream = entry.Open())
            using (MemoryStream ms = new MemoryStream())
            {
                openedStream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static string ReadAllText(this ZipArchiveEntry entry)
        {
            using (Stream openedStream = entry.Open())
            using (StreamReader sr = new StreamReader(openedStream))
                return sr.ReadToEnd();
        }
    }
}
