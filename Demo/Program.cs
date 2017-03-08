using System;
using System.Threading;
using System.Threading.Tasks;
using Metrics;
using Metrics.MetricData;
using Metrics.NET.PerformanceCounters;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Monitoring;
using NServiceBus.Raw;
using ServiceControl.Monitoring;
using Monitor = ServiceControl.Monitoring.Monitor;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            PerformanceCounters.EnsureCreated();

            Start().GetAwaiter().GetResult();
        }

        static async Task Start()
        {
            var monitor = await StartMonitor().ConfigureAwait(false);

            var receiverConfig = PrepareConfiguration("Receiver");
            var anotherReceiverConfig = PrepareConfiguration("AnotherReceiver");
            var senderConfig = PrepareConfiguration("Sender");

            var receiver = await Endpoint.Start(receiverConfig).ConfigureAwait(false);
            var sender = await Endpoint.Start(senderConfig).ConfigureAwait(false);
            var anotherReceiver = await Endpoint.Start(anotherReceiverConfig).ConfigureAwait(false);
            
            var tokenSource = new CancellationTokenSource();
            var senderTask = Task.Run(() => Sender(sender, tokenSource.Token));

            await receiver.Subscribe<MyEvent>().ConfigureAwait(false);
            await anotherReceiver.Subscribe<MyEvent>().ConfigureAwait(false);

            Console.WriteLine("Press <enter> to exit.");
            Console.ReadLine();

            tokenSource.Cancel();

            await senderTask.ConfigureAwait(false);
            await sender.Stop().ConfigureAwait(false);
            await receiver.Stop().ConfigureAwait(false);
            await anotherReceiver.Stop().ConfigureAwait(false);
            await monitor.Stop();
        }

        static EndpointConfiguration PrepareConfiguration(string endpointName)
        {
            var config = new EndpointConfiguration(endpointName);
            config.UsePersistence<InMemoryPersistence>();
            config.SendFailedMessagesTo("error");
            config.LimitMessageProcessingConcurrencyTo(1);
            config.EnableQueueLengthMonitorPlugin("QueueLengthMonitor");
            config.DisableFeature<AutoSubscribe>();
            ConfigureTransportAndRouting(config);
            return config;
        }

        static async Task<IRawEndpointInstance> StartMonitor()
        {
            var queueMonitorContext = new DefaultMetricsContext("QueueLengthMonitor");
            new MetricsConfig(queueMonitorContext)
                .WithHttpEndpoint("http://localhost:7777/QueueLengthMonitor/")
                .WithReporting(r =>
                {
                    r.WithReport(
                        new PerformanceCounterReporter(x => new CounterInstanceName("Queue Length", x.MetricName)),
                        TimeSpan.FromSeconds(5), Filter.New.WhereContext(c =>  c == "QueueLengthMonitor" || c == "QueueState"));
                });

            var monitor = new Monitor(queueMonitorContext, new QueueLength());
            var config = RawEndpointConfiguration.Create("QueueLengthMonitor", monitor.OnMessage);
            config.LimitMessageProcessingConcurrencyTo(1);
            config.UseTransport<MsmqTransport>();
            //config.UseTransport<RabbitMQTransport>().ConnectionString("host=localhost");
            config.SendFailedMessagesTo("error");

            var endpoint = await RawEndpoint.Start(config);
            return endpoint;
        }

        class QueueLengthFilter : MetricsFilter
        {
            public bool IsMatch(string context)
            {
                return false;
            }

            public bool IsMatch(GaugeValueSource gauge)
            {
                return false;
            }

            public bool IsMatch(CounterValueSource counter)
            {
                return false;
            }

            public bool IsMatch(MeterValueSource meter)
            {
                return false;
            }

            public bool IsMatch(HistogramValueSource histogram)
            {
                return false;
            }

            public bool IsMatch(TimerValueSource timer)
            {
                return false;
            }
        }

        static void ConfigureTransportAndRouting(EndpointConfiguration config)
        {
            //config.UseTransport<RabbitMQTransport>().ConnectionString("host=localhost");
            var routing = config.UseTransport<MsmqTransport>().Routing();
            routing.RegisterPublisher(typeof(MyEvent), "Sender");
        }

        static async Task Sender(IMessageSession session, CancellationToken token)
        {
            const int repeats = 10;
            int counter = 0;
            var delaySequence = new[]
            {
                500,
                500,
                500,
                500,
                1000,
                2000,
                1000
            };

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                await Step(session, delaySequence[counter % delaySequence.Length], repeats, token);
                counter++;
            }
        }

        static async Task Step(IMessageSession session, int delay, int repeats, CancellationToken token)
        {
            for (var i = 0; i < repeats; i++)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                var options = new SendOptions();
                options.SetDestination("Receiver");
                await session.Send(new MyMessage(), options).ConfigureAwait(false);
                await session.Publish(new MyEvent()).ConfigureAwait(false);
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }

    class MyMessage : IMessage
    {
    }

    class MyEvent : IEvent
    {
        
    }

    class MyEventHandler : IHandleMessages<MyEvent>
    {
        public Task Handle(MyEvent message, IMessageHandlerContext context)
        {
            return Task.Delay(2000);
        }
    }

    class MyMessageHandler : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            return Task.Delay(1000);
        }
    }
}
