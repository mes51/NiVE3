using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NiVE3.Data;
using NiVE3.Input;
using NiVE3.Plugin.ValueObject;
using NiVE3.Property.Types;
using NiVE3.Text;
using NiVE3.SourceGenerator.ReactivePropertyGenerator;
using Prism.Mvvm;
using SixLabors.Fonts;

namespace NiVE3.Model.UI
{
    [UseReactiveProperty]
    partial class TextPropertyModel : BindableBase
    {
        const string DefaultFontName = "游ゴシック";

        const string DefaultFontSubFamilyName = "Regular";

        [ReactiveProperty]
        public partial FontInfo SelectedFont { get; set; }

        [ReactiveProperty]
        public partial double FontSize { get; set; } = 20.0;

        [ReactiveProperty]
        public partial double LineHeight { get; set; } = 1.0;

        [ReactiveProperty]
        public partial double VerticalScale { get; set; } = 100.0;

        [ReactiveProperty]
        public partial double HorizontalScale { get; set; } = 100.0;

        [ReactiveProperty]
        public partial double LetterSpacing { get; set; }

        [ReactiveProperty]
        public partial double TextLineWidth { get; set; }

        [ReactiveProperty]
        public partial TextLineDrawOrder TextLineDrawOrder { get; set; }

        [ReactiveProperty]
        public partial bool IsEnableBold { get; set; }

        [ReactiveProperty]
        public partial bool IsEnableItalic { get; set; }

        [ReactiveProperty]
        public partial TextAlign TextAlign { get; set; }

        [ReactiveProperty]
        public partial FloatColor FillColor { get; set; } = FloatColor.White;

        [ReactiveProperty]
        public partial FloatColor TextLineColor { get; set; } = new FloatColor(1.0F, 0.0F, 0.0F, 1.0F);

        public FontGroup[] FontGroups { get; }

        FontCollection FontCollection { get; } = new FontCollection();

        public TextPropertyModel()
        {
            FontGroups = [..FontInfo.LoadedFonts.GroupBy(f => f.Name).Select(g => new FontGroup([.. g])).OrderBy(g => g.FontName)];
            SelectedFont = FontGroups.FirstOrDefault(g => g.FontName == DefaultFontName)?.SubFamiles?.TryGetValue(DefaultFontSubFamilyName, out var defaultFont) ?? false ? defaultFont : FontInfo.FallbackFont;
        }

        public TextStyle GetStyle()
        {
            return new TextStyle(
                SelectedFont.UniqueId,
                (float)FontSize,
                (float)LineHeight,
                (float)LetterSpacing,
                (float)VerticalScale,
                (float)HorizontalScale,
                TextLineDrawOrder,
                (float)TextLineWidth,
                IsEnableBold,
                IsEnableItalic,
                TextAlign,
                (Vector4)FillColor,
                (Vector4)TextLineColor
            );
        }

        public void SetStyle(TextStyle style)
        {
            SelectedFont = FontInfo.FindByUniqueId(style.FontUniqueId) ?? FontInfo.FallbackFont;
            FontSize = style.FontSize;
            LineHeight = style.LineHeight;
            LetterSpacing = style.LetterSpacing;
            VerticalScale = style.VerticalScale;
            HorizontalScale = style.HorizontalScale;
            TextLineDrawOrder = style.TextLineDrawOrder;
            TextLineWidth = style.TextLineWidth;
            IsEnableBold = style.IsEnableBold;
            IsEnableItalic = style.IsEnableItalic;
            TextAlign = style.TextAlign;
            FillColor = (FloatColor)style.FillColor;
            TextLineColor = (FloatColor)style.TextLineColor;
        }

        public void UpdateTextProperty(LayerModel targetLayer, Time layerTime, object? prevValue)
        {
            var currentText = prevValue as StyledText ?? (targetLayer.GetTextProperties(layerTime)?.TryGetValueInTree(TextFootageSource.SourceTextId, out var styledText) ?? false ? styledText as StyledText ?? StyledText.Empty : StyledText.Empty);
            var newStyle = GetStyle();
            targetLayer.UpdateTextProperty(TextFootageSource.SourceTextId, SourceTextPropertyType.ReplaceDefaultStyle(currentText, newStyle), currentText, layerTime);
        }
    }

    record FontGroup(FontInfo[] FontInfos)
    {
        public string FontName { get; } = FontInfos.FirstOrDefault()?.Name ?? "";

        public Dictionary<string, FontInfo> SubFamiles { get; } = FontInfos.GroupBy(i =>
        {
            if (string.IsNullOrEmpty(i.TypographicSubFamilyName))
            {
                return i.SubFamilyName;
            }
            else
            {
                return i.TypographicSubFamilyName;
            }
        }).ToDictionary(g => g.Key, g => g.Last());
    }
}
