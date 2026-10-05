using CalcioAnalytic.Analytics;
using CalcioAnalytic.Application;
using CalcioAnalytic.Infrastructure;
using CalcioAnalytic.Ingestion;
using CalcioAnalytic.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Register every layer the post-match worker depends on. AddInfrastructure wires
// up the DbContext (using the "Postgres" connection string from configuration),
// repositories, the unit of work, and the clock; the remaining calls register the
// ingestion/settlement/analysis services and the mock provider.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddIngestion();
builder.Services.AddMockProvider();
builder.Services.AddSportmonksProvider();
builder.Services.AddAnalytics();

// Bind worker options from the "Worker" section (falls back to defaults if absent).
builder.Services.Configure<WorkerOptions>(
    builder.Configuration.GetSection(WorkerOptions.SectionName));

builder.Services.AddHostedService<PostMatchProcessingWorker>();
builder.Services.AddHostedService<SportmonksSyncWorker>();
builder.Services.AddHostedService<FeatureSnapshotPopulationWorker>();

var host = builder.Build();
host.Run();
