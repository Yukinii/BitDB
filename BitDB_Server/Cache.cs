using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Timers;
using BitDB;
using BitDB_Server.IO;

namespace BitDB_Server
{

    public static class Cache
    {
        public static readonly ConcurrentDictionary<string, INI> CachedFiles = new ConcurrentDictionary<string, INI>();
        public static readonly ConcurrentDictionary<string, INI> Cleanup = new ConcurrentDictionary<string, INI>();
        public static readonly Timer AutoFlusTimer = new Timer(1000);
        public static bool Initialized;
        private static void Initialize()
        {
            Initialized = true;
            AutoFlusTimer.Elapsed += PerformFlush;
            AutoFlusTimer.Start();
        }
        public static INI CacheLookup(string file)
        {
            if (!Initialized)
                Initialize();

            INI iniFile;
            if (CachedFiles.TryGetValue(file, out iniFile))
            {
                Write("#", ConsoleColor.Green);
                return iniFile;
            }

            iniFile = new INI(file);
            CachedFiles.TryAdd(file, iniFile);
            Write("#", ConsoleColor.Red);
            return iniFile;
        }

        public static void Write(object text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        private static void PerformFlush(object sender, ElapsedEventArgs e)
        {
            foreach (var cachedFile in CachedFiles.Where(cachedFile => cachedFile.Value.RecentlyAccessed.AddSeconds(30) <= DateTime.UtcNow))
            {
                cachedFile.Value.Flush();
                if (Cleanup.ContainsKey(cachedFile.Key))
                {
                    INI bla;
                    Cleanup.TryRemove(cachedFile.Key, out bla);
                }
                else
                {
                    Cleanup.TryAdd(cachedFile.Key, cachedFile.Value);
                }
            }
            foreach (var ini in Cleanup)
            {
                INI bla;
                CachedFiles.TryRemove(ini.Key, out bla);
            }
            Cleanup.Clear();
        }
    }
}