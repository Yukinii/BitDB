using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BitDB_Server.IO
{
    public class INI
    {
        #region "Declarations"

        private string _fileName;
        public DateTime RecentlyAccessed;
        private bool _cacheModified;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _sections = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _modified = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

        #endregion

        #region "Methods"

        public INI(string fileName)
        {
            Initialize(fileName);
        }

        private void Initialize(string fileName)
        {
            RecentlyAccessed = DateTime.UtcNow;
            _fileName = fileName;
            Refresh();
        }

        private static string ParseSectionName(string line)
        {
            if (!line.StartsWith("[", StringComparison.Ordinal)) return null;
            if (!line.EndsWith("]", StringComparison.Ordinal)) return null;
            return line.Length < 3 ? null : line.Substring(1, line.Length - 2);
        }

        private static bool ParseKeyValuePair(string line, ref string key, ref string value)
        {
            int I;
            if ((I = line.IndexOf('=')) <= 0) return false;

            var j = line.Length - I - 1;
            key = line.Substring(0, I).Trim();
            if (key.Length <= 0) return false;

            value = (j > 0) ? (line.Substring(I + 1, j).Trim()) : ("");
            return true;
        }

        private void Refresh()
        {
            StreamReader sr = null;
            try
            {
                _sections.Clear();
                _modified.Clear();
                try
                {
                    sr = !File.Exists(_fileName) ? new StreamReader(File.Create(_fileName)) : new StreamReader(_fileName);
                }
                catch
                {
                    return;
                }

                ConcurrentDictionary<string, string> currentSection = null;
                string s;
                string key = null;
                string value = null;
                while ((s = sr.ReadLine()) != null)
                {
                    s = s.Trim();
                    var sectionName = ParseSectionName(s);
                    if (sectionName != null)
                    {
                        if (_sections.ContainsKey(sectionName))
                            currentSection = null;
                        else
                        {
                            currentSection = new ConcurrentDictionary<string, string>();
                            _sections.TryAdd(sectionName, currentSection);
                        }
                    }
                    else if (currentSection != null)
                    {
                        if (!ParseKeyValuePair(s, ref key, ref value)) continue;

                        if (!currentSection.ContainsKey(key))
                            currentSection.TryAdd(key, value);
                    }
                }
            }
            finally
            {
                sr?.Close();
            }
        }

        public void Flush() => PerformFlush();

        private async void PerformFlush()
        {
            try
            {
                if (!_cacheModified)
                    return;
                _cacheModified = false;
                var originalFileExists = File.Exists(_fileName);
                var tmpFileName = Path.ChangeExtension(_fileName, "$n$");
                if (!Directory.Exists(Path.GetDirectoryName(tmpFileName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(tmpFileName));
                while (File.Exists(tmpFileName))
                {
                    try
                    {
                        File.Delete(tmpFileName);
                    }
                    catch
                    {
                        Console.WriteLine("Fail.");
                    }
                }
                var sw = new StreamWriter(tmpFileName) { AutoFlush = true };

                try
                {
                    ConcurrentDictionary<string, string> currentSection = null;
                    if (originalFileExists)
                    {
                        StreamReader sr = null;
                        try
                        {
                            sr = new StreamReader(_fileName);
                            string key = null;
                            string value = null;
                            var reading = true;
                            while (reading)
                            {
                                var s = sr.ReadLine();
                                reading = (s != null);
                                bool unmodified;
                                string sectionName;
                                if (reading)
                                {
                                    unmodified = true;
                                    s = s.Trim();
                                    sectionName = ParseSectionName(s);
                                }
                                else
                                {
                                    unmodified = false;
                                    sectionName = null;
                                }

                                if ((sectionName != null) || (!reading))
                                {
                                    if (currentSection?.Count > 0)
                                    {
                                        var section = currentSection;
                                        foreach (var fkey in currentSection.Keys.Where(fkey => section.TryGetValue(fkey, out value)))
                                        {
                                            sw.Write(fkey);
                                            sw.Write('=');
                                            sw.WriteLine(value);
                                        }
                                        currentSection.Clear();
                                    }

                                    if (reading)
                                    {
                                        if (!_modified.TryGetValue(sectionName, out currentSection))
                                        {
                                        }
                                    }
                                }
                                else if (currentSection != null)
                                {
                                    if (ParseKeyValuePair(s, ref key, ref value))
                                    {
                                        if (currentSection.TryGetValue(key, out value))
                                        {
                                            unmodified = false;
                                            string bla;
                                            while (!currentSection.TryRemove(key,out bla))
                                            {
                                                await Task.Delay(10);
                                            }
                                            sw.Write(key);
                                            sw.Write('=');
                                            sw.WriteLine(value);
                                        }
                                    }
                                }
                                if (unmodified)
                                    sw.WriteLine(s);
                            }
                        }
                        finally
                        {
                            sr.Close();
                        }
                    }

                    foreach (var sectionPair in _modified)
                    {
                        currentSection = sectionPair.Value;
                        if (currentSection.Count <= 0)
                            continue;
                        sw.Write('[');
                        sw.Write(sectionPair.Key);
                        sw.WriteLine(']');
                        foreach (var valuePair in currentSection)
                        {
                            sw.Write(valuePair.Key);
                            sw.Write('=');
                            sw.WriteLine(valuePair.Value);
                        }
                        currentSection.Clear();
                    }
                    _modified.Clear();

                    sw.Close();
                    File.Copy(tmpFileName, _fileName, true);
                    File.Delete(tmpFileName);
                }
                finally
                {
                    // ReSharper disable once ConstantConditionalAccessQualifier
                    sw?.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // *** Read a value from local cache ***
        public string ReadString(string sectionName, string key, string defaultValue)
        {
            RecentlyAccessed = DateTime.UtcNow;
            ConcurrentDictionary<string, string> section;
            if (!_sections.TryGetValue(sectionName, out section)) return defaultValue;
            string value;
            return !section.TryGetValue(key, out value) ? defaultValue : value;
        }
        public async void Write(string sectionName, string key, object value)
        {
            _cacheModified = true;
            RecentlyAccessed = DateTime.UtcNow;
            ConcurrentDictionary<string, string> section;
            if (!_sections.TryGetValue(sectionName, out section))
            {
                section = new ConcurrentDictionary<string, string>();
                _sections.TryAdd(sectionName, section);
            }

            if (section.ContainsKey(key))
            {
                string bla;
                while (!section.TryRemove(key,out bla))
                {
                    await Task.Delay(10);
                }
            }
            section.TryAdd(key, Convert.ToString(value));

            if (!_modified.TryGetValue(sectionName, out section))
            {
                section = new ConcurrentDictionary<string, string>();
                _modified.TryAdd(sectionName, section);
            }

            if (section.ContainsKey(key))
            {
                string bla;
                while (!section.TryRemove(key,out bla))
                {
                    await Task.Delay(10);
                }
            }
            section.TryAdd(key, Convert.ToString(value));
        }

        public bool GetValue(string sectionName, string key, bool defaultValue)
        {
            var stringValue = ReadString(sectionName, key, defaultValue.ToString(CultureInfo.InvariantCulture));
            int value;
            if (int.TryParse(stringValue, out value)) return (value != 0);
            return defaultValue;
        }

        public int ReadInt(string sectionName, string key, int defaultValue)
        {
            var stringValue = ReadString(sectionName, key, defaultValue.ToString(CultureInfo.InvariantCulture));
            int value;
            return int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? value : defaultValue;
        }

        public byte ReadByte(string sectionName, string key, byte defaultValue)
        {
            var stringValue = ReadString(sectionName, key, defaultValue.ToString(CultureInfo.InvariantCulture));
            byte value;
            return byte.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? value : defaultValue;
        }

        public ushort ReadShort(string sectionName, string key, ushort defaultValue)
        {
            var stringValue = ReadString(sectionName, key, defaultValue.ToString(CultureInfo.InvariantCulture));
            ushort value;
            return ushort.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? value : defaultValue;
        }
        public double ReadDouble(string sectionName, string key, double defaultValue = 0)
        {
            var stringValue = ReadString(sectionName, key, defaultValue.ToString(CultureInfo.InvariantCulture));
            double value;
            return double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? value : defaultValue;
        }
        public DateTime GetValue(string sectionName, string key, DateTime defaultValue)
        {
            var stringValue = ReadString(sectionName, key, defaultValue.ToString(CultureInfo.InvariantCulture));
            DateTime value;
            return DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AssumeLocal, out value) ? value : defaultValue;
        }

        // *** Setters for various types ***
        public void SetValue(string sectionName, string key, bool value) => Write(sectionName, key, (value) ? ("1") : ("0"));

        public void SetValue(string sectionName, string key, int value) => Write(sectionName, key, value.ToString(CultureInfo.InvariantCulture));

        public void SetValue(string sectionName, string key, DateTime value) => Write(sectionName, key, value.ToString(CultureInfo.InvariantCulture));

        #endregion

        ~INI()
        {
            Write("#", ConsoleColor.Blue);
        }

        public static void Write(object text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }
    }
}
