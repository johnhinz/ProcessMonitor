using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Datadog.Logs;

namespace ProcessWatcher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddLogging(loggingBuilder =>
                        loggingBuilder.AddSerilog(
                            new LoggerConfiguration()
                                .WriteTo.DatadogLogs(
                                    apiKey: "API KEY GOES Here",
                                    host: Environment.MachineName,
                                    source: "QuartzProcessMonitor",
                                    service: "QuartzProcessMonitor",
                                    configuration: new DatadogConfiguration(),
                                    logLevel: LogEventLevel.Information
                                )
                                .CreateLogger())); 

                });
    }
}
