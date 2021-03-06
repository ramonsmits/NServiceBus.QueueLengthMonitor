﻿using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Monitoring
{
    class MarkerBehavior : Behavior<IDispatchContext>
    {
        Marker marker;

        public MarkerBehavior(Marker marker)
        {
            this.marker = marker;
        }

        public override Task Invoke(IDispatchContext context, Func<Task> next)
        {
            foreach (var operation in context.Operations)
            {
                marker.Mark(operation);
            }
            return next();
        }
    }
}
