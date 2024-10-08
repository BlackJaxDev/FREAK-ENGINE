using System.Text;
using XREngine.Data;

namespace XREngine.Core.Files
{
    [XR3rdPartyExtensions("txt")]
    public class TextFile : XRAsset
    {
        public event Action? TextChanged;

        private string? _text = null;
        public string? Text
        {
            get => _text;
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

        public static TextFile FromText(string text)
            => new() { Text = text };

        public static implicit operator string?(TextFile textFile)
            => textFile?.Text;
        public static implicit operator TextFile(string text)
            => FromText(text);

        public unsafe void LoadTextFileMapped(string path)
        {
            using FileMap map = FileMap.FromFile(path, FileMapProtect.Read);
            Encoding = GetEncoding(map, out int bomLength);
            _text = Encoding.GetString((byte*)map.Address + bomLength, map.Length - bomLength);
        }

        public async Task LoadTextAsync(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                _text = await File.ReadAllTextAsync(path, Encoding = GetEncoding(path));
        }
        public void LoadText(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                _text = File.ReadAllText(path, Encoding = GetEncoding(path));
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

#pragma warning disable SYSLIB0001 // Type or member is obsolete
            if (bom[0] == 0x2B && bom[1] == 0x2F && bom[2] == 0x76) return Encoding.UTF7;
#pragma warning restore SYSLIB0001 // Type or member is obsolete
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
#pragma warning disable SYSLIB0001 // Type or member is obsolete
                return Encoding.UTF7;
#pragma warning restore SYSLIB0001 // Type or member is obsolete
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

        public override void Load3rdParty(string filePath)
            => LoadText(filePath);
        public override async Task Load3rdPartyAsync(string filePath)
            => await LoadTextAsync(filePath);

        public void SaveTo(string path)
            => File.WriteAllText(path, _text ?? string.Empty, Encoding);

        public async Task SaveToAsync(string path)
            => await File.WriteAllTextAsync(path, _text ?? string.Empty, Encoding);
    }
}