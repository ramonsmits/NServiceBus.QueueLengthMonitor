using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    abstract class Reporter : FeatureStartupTask
    {
        protected Reporter(IAsyncTimer timer, IDispatchMessages dispatcher, string monitorAddress, string reportName)
        {
            this.timer = timer;
            this.dispatcher = dispatcher;
            this.monitorAddress = monitorAddress;
            this.reportName = reportName;
        }

        protected override Task OnStart(IMessageSession session)
        {
            timer.Start(Report, TimeSpan.FromSeconds(5), OnError);
            return Task.CompletedTask;
        }

        void OnError(Exception error)
        {
            log.Error("Unhandled exception when reporting link state", error);
        }

        protected abstract object GenerateReport();

        Task Report()
        {
            return Send(GenerateReport());
        }

        Task Send(object report)
        {
            var serialized = JsonConvert.SerializeObject(report);
            var body = Encoding.UTF8.GetBytes(serialized);

            var headers = new Dictionary<string, string>();
            headers["NServiceBus.QueueLength.Type"] = reportName;
            var message = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
            var operation = new TransportOperation(message, new UnicastAddressTag(monitorAddress));
            return dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag());
        }

        protected override Task OnStop(IMessageSession session)
        {
            return timer.Stop();
        }

        IAsyncTimer timer;
        IDispatchMessages dispatcher;
        string monitorAddress;
        string reportName;
        ILog log = LogManager.GetLogger<SourceReporter>();
    }
}