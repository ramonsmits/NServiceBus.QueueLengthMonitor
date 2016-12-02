using System;
using System.Threading.Tasks;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    interface IAsyncTimer
    {
        void Start(Func<Task> callback, TimeSpan interval, Action<Exception> errorCallback);
        Task Stop();
    }
}