using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NiVE3.View.Resource
{
    static class UIParameters
    {
        // TODO: デザイン決定後調整
        public const double TimelineRangeThumbWidth = 10.0;

        public const double TimelineRangeThumbTotalWidth = TimelineRangeThumbWidth * 2.0;

        public const double TimeLocatorTimeBarHeight = 20.0;

        public const double ArrowWidth = 19.0;

        public const double TagAreaWidth = 19.0;

        public const double AVSwitchWidthWithHalfSplitter = 68; // 66 + ceil(pr:ItemResizableStackPanel.SplitterSize / 2)

        public const double LayerUIHeight = 17.0;

        public static readonly GridLength ArrowWidthGridLength = new GridLength(ArrowWidth);

        public static readonly GridLength LayerUIHeightGridLength = new GridLength(LayerUIHeight);

        public static readonly GridLength TimeLocatorTimeBarHeightGridLength = new GridLength(TimeLocatorTimeBarHeight);

        public static readonly GridLength VerticalScrollBarWidthGridLength = new GridLength(SystemParameters.VerticalScrollBarWidth);

        public static readonly GridLength HorizontalScrollBarWidthGridLength = new GridLength(SystemParameters.HorizontalScrollBarHeight);
    }
}
