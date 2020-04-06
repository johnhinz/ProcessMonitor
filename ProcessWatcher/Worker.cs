using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProcessWatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _watchedProcess;
        private readonly string _killParentProcess;
        private readonly string _killChildProcess;
        private readonly int _historicalBounds;
        private DateTime _latestProcess;

        public Worker(ILogger<Worker> logger)
        {
            _latestProcess = DateTime.Now - TimeSpan.FromMinutes(_historicalBounds);
            _logger = logger;
            _watchedProcess = "svchost";
            _killParentProcess = "??";
            _killChildProcess = "??";
            _historicalBounds = 1;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(10000, stoppingToken);

                IEnumerable<Process> allProcesses = Process.GetProcesses().Where(p => p.ProcessName == _watchedProcess);
                
                Parallel.ForEach(allProcesses, (process) =>
                {
                    ManagementBaseObject[] subProcesses = GetChildren(process.Id);
                    Parallel.ForEach(subProcesses.Where(p => p != null), (ManagementBaseObject mo) =>
                    {
                        Process moProcess = Process.GetProcessById((int)(uint)mo["ProcessID"]);
                        if (moProcess.StartTime > _latestProcess)
                        {
                            _latestProcess = moProcess.StartTime;
                        }
                        _logger.LogInformation($@"{process.ProcessName} - {moProcess.StartTime} - {mo["ProcessID"]} - {moProcess.ProcessName} - {mo.Properties["CommandLine"].Value}");
                    });
                });

                if (_latestProcess < DateTime.Now.Subtract(TimeSpan.FromMinutes(_historicalBounds)))
                {
                    int parentProcessID = 0;
                    
                    IEnumerable<Process> killProcesses = Process.GetProcesses().Where(p => p.ProcessName == _killParentProcess);
                    foreach(var process in allProcesses)
                    {
                        ManagementBaseObject[] subProcesses = GetChildren(process.Id);
                        foreach(var child in subProcesses )
                        {
                            if (child != null)
                            {
                                if (_killChildProcess == (string)child.Properties["CommandLine"].Value)
                                {
                                    _logger.LogInformation($"Killing {process.Id} {process.ProcessName}");
                                }
                            }
                       
                        }
                    }
                }
            }
        }

        protected ManagementBaseObject[] GetChildren(int parentProcessID)
        {
            ManagementObjectSearcher mos = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", parentProcessID));
            ManagementObjectCollection childProcesses = mos.Get();
            ManagementBaseObject[] subProcesses = new ManagementBaseObject[childProcesses.Count + 1];
            childProcesses.CopyTo(subProcesses, 0);
            return subProcesses;
        }
    }
}
