using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace NiVE3.Wpf.Interaction.Trigger
{
    class InteractionRequestTrigger : Microsoft.Xaml.Behaviors.EventTrigger
    {
        protected override string GetEventName()
        {
            return nameof(InteractionRequest.Raised);
        }
    }

    class InteractionRequest
    {
        public event EventHandler<EventArgs>? Raised;

        public void Raise()
        {
            Raised?.Invoke(this, EventArgs.Empty);
        }
    }
}
