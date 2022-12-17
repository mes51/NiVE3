using AvalonDock;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.Region
{
    class DockingManagerRegionAdapter : RegionAdapterBase<DockingManager>
    {
        IRegion? Region { get; set; }

        DockingManager? RegionTarget { get; set; }

        public DockingManagerRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory) : base(regionBehaviorFactory) { }

        protected override void Adapt(IRegion region, DockingManager regionTarget)
        {
            if (region == null)
            {
                throw new ArgumentException(nameof(region));
            }
            if (regionTarget == null)
            {
                throw new ArgumentException(nameof(regionTarget));
            }

            Region = region;
            RegionTarget = regionTarget;
        }

        protected override IRegion CreateRegion()
        {
            var region = new Prism.Regions.Region();
            region.SortComparison = null;
            return region;
        }

        protected override void AttachBehaviors(IRegion region, DockingManager regionTarget)
        {
            if (region == null)
            {
                throw new ArgumentException(nameof(region));
            }
            if (regionTarget == null)
            {
                throw new ArgumentException(nameof(regionTarget));
            }

            region.Behaviors.Add(DockingManagerRegionBehavior.BehaviorName, new DockingManagerRegionBehavior { RegionTarget = regionTarget });

            base.AttachBehaviors(region, regionTarget);
        }
    }
}
