using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace XREngine
{
    public class TextFile
    {
        public event Action TextChanged;

        public string? FilePath { get; set; }

        public static ConcurrentDictionary<string, (DateTime, string?)> TextCache { get; } = new();

        private string? _text = null;
        public string? Text
        {
            get => _text ?? LoadText();
            set
            {
                _text = value;
                OnTextChanged();
            }
        }

        private Encoding _encoding = Encoding.Default;
        public Encoding Encoding
        {
            get
            {
                if (_text is null && !string.IsNullOrWhiteSpace(FilePath))
                    _encoding = GetEncoding(FilePath);
                return _encoding;
            }
            set
            {
                _encoding = value;
            }
        }

        protected void OnTextChanged() => TextChanged?.Invoke();

        public TextFile()
        {
            FilePath = null;
            _text = null;
        }
        public TextFile(string path)
        {
            FilePath = path;
            _text = null;
        }

        public static TextFile FromText(string? text)
            => new() { Text = text };

        public static implicit operator string?(TextFile textFile)
            => textFile?.Text;
        public static implicit operator TextFile(string? text)
            => FromText(text);

        public virtual void SaveText(string filePath)
        {
            try
            {
                File.WriteAllText(filePath, Text, Encoding);
                DateTime lastUpdatedTime = File.GetLastWriteTime(FilePath);
                var attrib = (lastUpdatedTime, _text);
                TextCache.AddOrUpdate(filePath, attrib, (x, y) => attrib);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public virtual string? LoadText()
        {
            _text = null;
            if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
            {
                DateTime lastUpdatedTime = File.GetLastWriteTime(FilePath);
                if (TextCache.TryGetValue(FilePath, out (DateTime, string?) value))
                {
                    var attrib = value;
                    if (attrib.Item1 < lastUpdatedTime)
                    {
                        Read();
                        attrib.Item1 = lastUpdatedTime;
                        attrib.Item2 = _text;
                        TextCache[FilePath] = attrib;
                    }
                    else
                        _text = attrib.Item2;
                }
                else
                {
                    Read();
                    var attrib = (lastUpdatedTime, _text);
                    TextCache.AddOrUpdate(FilePath, attrib, (x, y) => attrib);
                }
            }

            return _text;
        }

        private unsafe void Read()
        {
            if (FilePath is null)
                return;

            using FileMap map = FileMap.FromFile(FilePath, FileMapProtect.Read);
            Encoding = GetEncoding(map, out int bomLength);
            _text = Encoding.GetString((byte*)map.Address + bomLength, map.Length - bomLength);
        }

        public void UnloadText()
        {
            _text = null;
        }
        public async Task<string?> LoadTextAsync()
        {
            _text = null;
            if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
                _text = await Task.Run(() => File.ReadAllText(FilePath, Encoding = GetEncoding(FilePath)));
            return _text;
        }

        /// <summary>
        /// Determines a text file's encoding by analyzing its byte order mark (BOM).
        /// Defaults to ASCII when detection of the text file's endianness fails.
        /// </summary>
        /// <param name="path">The text file to analyze.</param>
        /// <returns>The detected encoding.</returns>
        public static Encoding GetEncoding(string path)
        {
            byte[] bom = new byte[4];
            using (FileMap map = FileMap.FromFile(path, FileMapProtect.Read, 0, 4))
                bom = map.Address.GetBytes(4);

            if (bom[0] == 0x2B && bom[1] == 0x2F && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) return Encoding.UTF8;
            if (bom[0] == 0xFF && bom[1] == 0xFE) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xFE && bom[1] == 0xFF) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xFE && bom[3] == 0xFF) return Encoding.UTF32;
            return Encoding.Default;
        }
        public static Encoding GetEncoding(FileMap file, out int bomLength)
        {
            byte[] bom = file.Address.GetBytes(4);
            if (bom[0] == 0x2B && bom[1] == 0x2F && bom[2] == 0x76)
            {
                bomLength = 3;
                return Encoding.UTF7;
            }
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                bomLength = 3;
                return Encoding.UTF8;
            }
            if (bom[0] == 0xFF && bom[1] == 0xFE)
            {
                bomLength = 2;
                return Encoding.Unicode; //UTF-16LE
            }
            if (bom[0] == 0xFE && bom[1] == 0xFF)
            {
                bomLength = 2;
                return Encoding.BigEndianUnicode; //UTF-16BE
            }
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xFE && bom[3] == 0xFF)
            {
                bomLength = 4;
                return Encoding.UTF32;
            }

            bomLength = 0;
            return Encoding.Default;
        }
    }
}
