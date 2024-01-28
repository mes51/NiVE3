using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using SixLabors.Fonts;
using SixLabors.Fonts.WellKnownIds;

namespace NiVE3.Model
{
    class TextPropertyModel : BindableBase
    {
        static readonly string[] FontExtensions = new string[] { "*.ttf", "*.ttc", "*.otf" };

        static readonly string UserFontFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Windows\\Fonts");

        static readonly string AdobeFontsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adobe\\CoreSync\\plugins\\livetype\\r");

        public FontInfo[] Fonts { get; }

        public FontGroup[] FontGroups { get; }

        FontCollection FontCollection { get; } = new FontCollection();

        public TextPropertyModel()
        {
            var fontFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var fontFiles = GetFontPaths(fontFolder);

            if (Directory.Exists(UserFontFolder))
            {
                fontFiles = fontFiles.Concat(GetFontPaths(UserFontFolder));
            }
            if (Directory.Exists(AdobeFontsFolder))
            {
                fontFiles = fontFiles.Concat(Directory.GetFiles(AdobeFontsFolder));
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

            Fonts = fontInfo.ToArray();
            FontGroups = fontInfo.GroupBy(f => f.Name).Select(g => new FontGroup(g.ToArray())).ToArray();
        }

        FontInfo[]? LoadFonts(string path)
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

                return result.ToArray();
            }
            catch { }

            return null;
        }

        FontInfo? LoadFont(string path)
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

    record FontInfo(FontFamily FontFamily, FontDescription Description)
    {
        public string Name { get; } = string.IsNullOrEmpty(Description.GetNameById(CultureInfo.CurrentCulture, KnownNameIds.TypographicFamilyName)) ?
            Description.FontFamily(CultureInfo.CurrentCulture) :
            Description.GetNameById(CultureInfo.CurrentCulture, KnownNameIds.TypographicFamilyName);

        public string UniqueId = Description.GetNameById(CultureInfo.InvariantCulture, KnownNameIds.UniqueFontID);
    }

    record FontGroup(FontInfo[] FontInfos)
    {
        public string FontName { get; } = FontInfos.FirstOrDefault()?.Name ?? "";

        public Dictionary<string, FontInfo> SubFamiles { get; } = FontInfos.ToDictionary(i =>
        {
            var typographic = i.Description.GetNameById(CultureInfo.CurrentCulture, KnownNameIds.TypographicSubfamilyName);
            var sub = i.Description.FontSubFamilyName(CultureInfo.CurrentCulture);

            if (string.IsNullOrEmpty(typographic))
            {
                return sub;
            }
            else
            {
                return typographic;
            }
        });
    }
}
