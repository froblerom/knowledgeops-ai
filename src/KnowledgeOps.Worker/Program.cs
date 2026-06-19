using KnowledgeOps.Application;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Infrastructure;
using KnowledgeOps.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Register only the Application services required for document processing.
// AddApplicationApiFeatures() is intentionally excluded: those services depend on
// ICurrentUser, which is an HTTP/JWT-request concept unavailable in the Worker host.
builder.Services.AddApplicationCore();
builder.Services.AddInfrastructure(builder.Configuration);

// Worker-specific services — do not call AddJwtInfrastructure(); Worker does not handle JWT.
builder.Services.AddScoped<ICorrelationContext, WorkerCorrelationContext>();
builder.Services.AddOptions<WorkerSettings>().BindConfiguration("Worker");
builder.Services.AddHostedService<DocumentProcessingWorker>();

var host = builder.Build();
host.Run();
