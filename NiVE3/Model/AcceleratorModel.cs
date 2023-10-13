using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using Prism.Mvvm;

namespace NiVE3.Model
{
    class AcceleratorModel : BindableBase, IDisposable
    {
        public Accelerator? Accelerator { get; }

        Context Context { get; }

        public AcceleratorModel()
        {
            Context = Context.Create(builder => builder.Cuda().Optimize(OptimizationLevel.O2).EnableAlgorithms());
            Accelerator = Context.GetPreferredDevices(false, true).FirstOrDefault(d => d.AcceleratorType != AcceleratorType.CPU)?.CreateAccelerator(Context);
        }

        public void Dispose()
        {
            Accelerator?.Dispose();
            Context.Dispose();
        }
    }
}
