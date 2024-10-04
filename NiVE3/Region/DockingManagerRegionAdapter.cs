using AvalonDock;
using CommunityToolkit.Diagnostics;
using Prism.Navigation.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Region
{
    class DockingManagerRegionAdapter : RegionAdapterBase<DockingManager>
    {
        public DockingManagerRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory) : base(regionBehaviorFactory) { }

        protected override void Adapt(IRegion region, DockingManager regionTarget) { }

        protected override IRegion CreateRegion()
        {
            var region = new Prism.Navigation.Regions.Region
            {
                SortComparison = null
            };
            return region;
        }

        protected override void AttachBehaviors(IRegion region, DockingManager regionTarget)
        {
            Guard.IsNotNull(region, nameof(region));
            Guard.IsNotNull(regionTarget, nameof(regionTarget));

            region.Behaviors.Add(DockingManagerRegionBehavior.BehaviorName, new DockingManagerRegionBehavior { RegionTarget = regionTarget });

            base.AttachBehaviors(region, regionTarget);
        }
    }
}
