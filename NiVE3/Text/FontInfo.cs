using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.Fonts.WellKnownIds;
using SixLabors.Fonts;
using System.IO;

namespace NiVE3.Text
{
    class FontInfo
    {
        static readonly string[] FontExtensions = ["*.ttf", "*.ttc", "*.otf"];

        static readonly string UserFontFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Windows\\Fonts");

        static readonly string[] AdobeFontsFolders = [..new string[] { "r", "e", "s", "w" }.Select(dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adobe\\CoreSync\\plugins\\livetype", dir))];

        public static FontInfo[] LoadedFonts { get; }

        public static FontInfo FallbackFont => LoadedFonts[0];

        static FontCollection FontCollection { get; } = new FontCollection();

        public FontFamily FontFamily { get; }

        public FontDescription Description { get; }

        public string Name { get; }

        public string TypographicSubFamilyName { get; }

        public string SubFamilyName { get; }

        public string UniqueId { get; }

        static FontInfo()
        {
            var fontFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var fontFiles = GetFontPaths(fontFolder);

            if (Directory.Exists(UserFontFolder))
            {
                fontFiles = fontFiles.Concat(GetFontPaths(UserFontFolder));
            }
            foreach (var adobeFontFolder in AdobeFontsFolders)
            {
                if (Directory.Exists(adobeFontFolder))
                {
                    fontFiles = fontFiles.Concat(Directory.GetFiles(adobeFontFolder));
                }
            }

            var fontInfo = new List<FontInfo>();
            foreach (var f in fontFiles)
            {
                var ext = Path.GetExtension(f);

                if (ext == "")
                {
                    var loadedFontInfos = LoadFonts(f);
                    if (loadedFontInfos != null)
                    {
                        fontInfo.AddRange(loadedFontInfos);
                        continue;
                    }

                    var loadedFontInfo = LoadFont(f);
                    if (loadedFontInfo != null)
                    {
                        fontInfo.Add(loadedFontInfo);
                    }
                }
                else if (ext == ".ttc")
                {
                    var loadedFontInfos = LoadFonts(f);
                    if (loadedFontInfos != null)
                    {
                        fontInfo.AddRange(loadedFontInfos);
                    }
                }
                else
                {
                    var loadedFontInfo = LoadFont(f);
                    if (loadedFontInfo != null)
                    {
                        fontInfo.Add(loadedFontInfo);
                    }
                }
            }

            LoadedFonts = [..fontInfo];
        }

        private FontInfo(FontFamily fontFamily, FontDescription description)
        {
            FontFamily = fontFamily;
            Description = description;

            Name = string.IsNullOrEmpty(Description.GetNameById(CultureInfo.CurrentCulture, KnownNameIds.TypographicFamilyName)) ?
                Description.FontFamily(CultureInfo.CurrentCulture) :
                Description.GetNameById(CultureInfo.CurrentCulture, KnownNameIds.TypographicFamilyName);
            TypographicSubFamilyName = Description.GetNameById(CultureInfo.CurrentCulture, KnownNameIds.TypographicSubfamilyName);
            SubFamilyName = Description.FontSubFamilyName(CultureInfo.CurrentCulture);
            UniqueId = Description.GetNameById(CultureInfo.InvariantCulture, KnownNameIds.UniqueFontID);
        }

        public static FontInfo? FindByUniqueId(string uniqueId)
        {
            return LoadedFonts.FirstOrDefault(i => i.UniqueId == uniqueId);
        }

        static FontInfo[]? LoadFonts(string path)
        {
            try
            {
                var loadedFontFamilies = FontCollection.AddCollection(path, out var descriptions).ToArray();

                var result = new List<FontInfo>();
                foreach (var d in descriptions)
                {
                    try
                    {
                        var fontFamily = loadedFontFamilies.First(f => d.FontFamilyInvariantCulture == f.Name);
                        result.Add(new FontInfo(fontFamily, d));
                    }
                    catch { }
                }

                return [..result];
            }
            catch { }

            return null;
        }

        static FontInfo? LoadFont(string path)
        {
            try
            {
                var loadedFontFamily = FontCollection.Add(path, out var description);
                return new FontInfo(loadedFontFamily, description);
            }
            catch { }

            return null;
        }

        static IEnumerable<string> GetFontPaths(string dir)
        {
            return FontExtensions.SelectMany(ext => Directory.GetFiles(dir, ext));
        }
    }
}
