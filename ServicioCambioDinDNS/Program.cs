using Quartz;
using ServicioCambioDinDNS;
using ServicioCambioDinDNS.Job;
using ServicioCambioDinDNS.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();



// registrar quartz -> registrar el job
builder.Services.AddQuartz(q => {
	q.AddJobAndTrigger<CheckDinDNS>(builder.Configuration);
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var host = builder.Build();
host.Run();
