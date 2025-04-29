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
using Prism.Mvvm;
using SixLabors.Fonts;

namespace NiVE3.Model.UI
{
    class TextPropertyModel : BindableBase
    {
        const string DefaultFontName = "游ゴシック";

        const string DefaultFontSubFamilyName = "Regular";

        private FontInfo selectedFont;
        public FontInfo SelectedFont
        {
            get { return selectedFont; }
            set { SetProperty(ref selectedFont, value); }
        }

        private double fontSize = 20.0;
        public double FontSize
        {
            get { return fontSize; }
            set { SetProperty(ref fontSize, value); }
        }

        private double lineHeight = 1.0;
        public double LineHeight
        {
            get { return lineHeight; }
            set { SetProperty(ref lineHeight, value); }
        }

        private double verticalScale = 100.0;
        public double VerticalScale
        {
            get { return verticalScale; }
            set { SetProperty(ref verticalScale, value); }
        }

        private double horizontalScale = 100.0;
        public double HorizontalScale
        {
            get { return horizontalScale; }
            set { SetProperty(ref horizontalScale, value); }
        }

        private double letterSpacing;
        public double LetterSpacing
        {
            get { return letterSpacing; }
            set { SetProperty(ref letterSpacing, value); }
        }

        private double textLineWidth;
        public double TextLineWidth
        {
            get { return textLineWidth; }
            set { SetProperty(ref textLineWidth, value); }
        }

        private TextLineDrawOrder textLineDrawOrder;
        public TextLineDrawOrder TextLineDrawOrder
        {
            get { return textLineDrawOrder; }
            set { SetProperty(ref textLineDrawOrder, value); }
        }

        private bool isEnableBold;
        public bool IsEnableBold
        {
            get { return isEnableBold; }
            set { SetProperty(ref isEnableBold, value); }
        }

        private bool isEnableItalic;
        public bool IsEnableItalic
        {
            get { return isEnableItalic; }
            set { SetProperty(ref isEnableItalic, value); }
        }

        private TextAlign textAlign;
        public TextAlign TextAlign
        {
            get { return textAlign; }
            set { SetProperty(ref textAlign, value); }
        }

        private FloatColor fillColor = FloatColor.White;
        public FloatColor FillColor
        {
            get { return fillColor; }
            set { SetProperty(ref fillColor, value); }
        }

        private FloatColor textLineColor = new FloatColor(1.0F, 0.0F, 0.0F, 1.0F);
        public FloatColor TextLineColor
        {
            get { return textLineColor; }
            set { SetProperty(ref textLineColor, value); }
        }

        public FontGroup[] FontGroups { get; }

        FontCollection FontCollection { get; } = new FontCollection();

#pragma warning disable CS8618 // 各フィールドには初期化時に必ず値を代入するため無視
        public TextPropertyModel()
#pragma warning restore CS8618
        {
            FontGroups = [.. FontInfo.LoadedFonts.GroupBy(f => f.Name).Select(g => new FontGroup([.. g])).OrderBy(g => g.FontName)];
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
