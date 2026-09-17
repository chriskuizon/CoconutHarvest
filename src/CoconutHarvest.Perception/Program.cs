using CoconutHarvest.Perception;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<PerceptionOptions>(builder.Configuration.GetSection(PerceptionOptions.Section));
builder.Services.AddSingleton<YoloDetector>();
builder.Services.AddSingleton<IRangeSource, NullRangeSource>();
builder.Services.AddHostedService<PerceptionWorker>();

await builder.Build().RunAsync();
