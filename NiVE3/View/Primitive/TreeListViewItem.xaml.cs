using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NiVE3.View.Primitive
{
    /// <summary>
    /// TreeListViewItem.xaml の相互作用ロジック
    /// </summary>
    public partial class TreeListViewItem : TreeViewItem
    {
        const int IndentWidth = 19;

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
            nameof(Columns),
            typeof(GridViewColumnCollection),
            typeof(TreeListViewItem),
            new FrameworkPropertyMetadata(new GridViewColumnCollection(), FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

        public GridViewColumnCollection Columns
        {
            get { return (GridViewColumnCollection)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        public TreeListViewItem()
        {
            InitializeComponent();
        }

        int level = -1;
        public int Level
        {
            get
            {
                if (level < 0)
                {
                    if (ItemsControlFromItemContainer(this) is TreeListViewItem parent)
                    {
                        level = parent.Level + 1;
                    }
                    else
                    {
                        level = 0;
                    }
                }
                return level;
            }
        }

        public double Indent => Level * IndentWidth;

        public bool IsToggleButtonElement(DependencyObject obj)
        {
            var scope = NameScope.GetNameScope(obj);
            var parent = obj;
            var parentTree = new List<DependencyObject>();
            do
            {
                parentTree.Add(parent);
                parent = VisualTreeHelper.GetParent(parent);
                if (parent == null || parent == this)
                {
                    return false;
                }
                scope = NameScope.GetNameScope(parent);

                if (scope != null)
                {
                    var expander = scope.FindName(TreeListExpandableGridViewColumn.ExpanderButtonName);
                    if (expander is DependencyObject e && parentTree.Contains(e))
                    {
                        return true;
                    }
                    else
                    {
                        scope = null;
                    }
                }
            }
            while (scope == null && parent != null);

            return false;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeListViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewItem;
        }

        static T? FindParent<T>(DependencyObject obj) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(obj);
            if (parent is T button)
            {
                return button;
            }

            if (parent != null)
            {
                return FindParent<T>(parent);
            }

            return null;
        }
    }
}
