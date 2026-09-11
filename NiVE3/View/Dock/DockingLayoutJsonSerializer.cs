using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvalonDock.Core;
using AvalonDock.Core.Serialization.Dto;
using AvalonDock.Serializer.Json;
using NiVE3.ViewModel;

namespace NiVE3.View.Dock
{
    /// <summary>
    /// メインウィンドウのドッキングレイアウトを JSON 形式でシリアライズするクラス
    /// </summary>
    /// <remarks>
    /// タイムラインパネルがレイアウト内に複数含まれてしまうことがあるため、シリアライズ時に最初の 1 つ以外を取り除く
    /// </remarks>
    class DockingLayoutJsonSerializer : JsonLayoutSerializer
    {
        static readonly string TimelineContentId = typeof(TimelineViewModel).Name;

        public DockingLayoutJsonSerializer(IDockingManager manager) : base(manager) { }

        protected override void SerializeCore(Stream stream, LayoutRootDto root)
        {
            RemoveDuplicatedTimelinePanes(root);
            base.SerializeCore(stream, root);
        }

        static void RemoveDuplicatedTimelinePanes(LayoutRootDto root)
        {
            var isFound = false;
            PruneChildren(root.RootPanel?.Children, ref isFound);
            foreach (var side in new[] { root.TopSide, root.RightSide, root.LeftSide, root.BottomSide })
            {
                PruneChildren(side?.Children, ref isFound);
            }
            PruneChildren(root.FloatingWindows, ref isFound);
            PruneChildren(root.Hidden, ref isFound);
        }

        /// <summary>
        /// 子要素から 2 つ目以降のタイムラインパネルを取り除く
        /// </summary>
        /// <returns>取り除いた結果、子要素が空になった場合は true</returns>
        static bool PruneChildren<T>(List<T>? children, ref bool isFound) where T : LayoutElementDto
        {
            if (children == null)
            {
                return false;
            }

            var isRemoved = false;
            var index = 0;
            while (index < children.Count)
            {
                if (ShouldRemove(children[index], ref isFound))
                {
                    children.RemoveAt(index);
                    isRemoved = true;
                }
                else
                {
                    index++;
                }
            }

            return isRemoved && children.Count == 0;
        }

        static bool ShouldRemove(LayoutElementDto element, ref bool isFound)
        {
            switch (element)
            {
                case LayoutContentDto content when content.ContentId == TimelineContentId:
                    if (isFound)
                    {
                        return true;
                    }
                    isFound = true;
                    return false;
                case LayoutPanelDto panel:
                    return PruneChildren(panel.Children, ref isFound);
                case LayoutAnchorablePaneGroupDto paneGroup:
                    return PruneChildren(paneGroup.Children, ref isFound);
                case LayoutAnchorablePaneDto pane:
                    return PruneChildren(pane.Children, ref isFound);
                case LayoutDocumentPaneGroupDto paneGroup:
                    return PruneChildren(paneGroup.Children, ref isFound);
                case LayoutDocumentPaneDto pane:
                    return PruneChildren(pane.Children, ref isFound);
                case LayoutAnchorGroupDto anchorGroup:
                    return PruneChildren(anchorGroup.Children, ref isFound);
                case LayoutAnchorableFloatingWindowDto window:
                    return PruneChildren(window.RootPanel?.Children, ref isFound);
                case LayoutDocumentFloatingWindowDto window:
                    return PruneChildren(window.RootPanel?.Children, ref isFound);
                default:
                    return false;
            }
        }
    }
}