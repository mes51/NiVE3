using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.MediaFoundation;

namespace NiVE3.PresetPlugin.Internal.MediaFoundation
{
    class MFInitializer
    {
        public bool Initialized { get; }

        static MFInitializer? Instance { get; set; }

        public static void Initialize()
        {
            if (Instance == null || !Instance.Initialized)
            {
                Instance = new MFInitializer();
            }
        }

        private MFInitializer()
        {
            Initialized = MediaFactory.MFStartup().Success;
        }

        ~MFInitializer()
        {
            if (Initialized)
            {
                MediaFactory.MFShutdown();
            }
        }
    }
}
