using System;
using System.Threading.Tasks;
using Metrics;
using NServiceBus.Raw;
using NServiceBus.Transport;

namespace NServiceBus.QueueLengthMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            Metric.Config
                .WithHttpEndpoint("http://localhost:7777/QueueLengthMonitor/")
                .WithReporting(r =>
                {
                    //r.WithConsoleReport(TimeSpan.FromSeconds(1));
                });

            var monitor = new Monitor();
            var config = RawEndpointConfiguration.Create("QueueLengthMonitor", monitor.OnMessage);
            config.UseTransport<MsmqTransport>();
            config.SendFailedMessagesTo("error");
            //config.LimitMessageProcessingConcurrencyTo(1);
            Start(config).GetAwaiter().GetResult();
        }

        static async Task Start(RawEndpointConfiguration config)
        {
            var endpoint = await RawEndpoint.Start(config);

            Console.WriteLine("Press <enter> to exit.");
            Console.ReadLine();

            await endpoint.Stop();
        }
    }

}
