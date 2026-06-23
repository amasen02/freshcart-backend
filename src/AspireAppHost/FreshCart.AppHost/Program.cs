// ---------------------------------------------------------------------------
//  FreshCart.AppHost
//  Aspire-orchestrated local boot. Backing services start as persistent
//  containers; FreshCart services run as .NET projects and inherit
//  connection strings + service-discovery names from this manifest.
//
//  Run:   dotnet run --project src/AspireAppHost/FreshCart.AppHost
//  Open:  http://localhost:15888  (Aspire dashboard)
// ---------------------------------------------------------------------------

var distributedApplicationBuilder = DistributedApplication.CreateBuilder(args);

// --- Relational stores ------------------------------------------------------

var sqlServer = distributedApplicationBuilder
    .AddSqlServer("sqlserver")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var identityDatabase = sqlServer.AddDatabase("identitydb");
var orderingDatabase = sqlServer.AddDatabase("orderingdb");
var inventoryDatabase = sqlServer.AddDatabase("inventorydb");
var paymentReadDatabase = sqlServer.AddDatabase("paymentreaddb");

var postgres = distributedApplicationBuilder
    .AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDatabase = postgres.AddDatabase("catalogdb");
var basketDatabase = postgres.AddDatabase("basketdb");

var mysql = distributedApplicationBuilder
    .AddMySql("mysql")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var reportingWarehouse = mysql.AddDatabase("reportingdb");

// --- Document store ---------------------------------------------------------

var mongo = distributedApplicationBuilder
    .AddMongoDB("mongodb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var deliveryDatabase = mongo.AddDatabase("deliverydb");
var paymentEventStore = mongo.AddDatabase("paymentevents");
var reviewsDatabase = mongo.AddDatabase("reviewsdb");
var supportChatTranscripts = mongo.AddDatabase("supportchatdb");
var notificationsDatabase = mongo.AddDatabase("notificationsdb");

// --- Cache + broker ---------------------------------------------------------

var distributedCache = distributedApplicationBuilder
    .AddRedis("cache")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var rabbitMq = distributedApplicationBuilder
    .AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// --- Services ---------------------------------------------------------------

var identityService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Identity_Api>("identity")
    .WithReference(identityDatabase)
    .WithReference(distributedCache)
    .WaitFor(identityDatabase)
    .WaitFor(distributedCache);

var catalogService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Catalog_Api>("catalog")
    .WithReference(catalogDatabase)
    .WithReference(distributedCache)
    .WithReference(rabbitMq)
    .WithEnvironment("MessageBroker__Host", rabbitMq.Resource.ConnectionStringExpression)
    .WaitFor(catalogDatabase)
    .WaitFor(rabbitMq);

// Pricing is a self-contained gRPC calculator backed by an embedded SQLite file,
// so it owns no Aspire-managed backing resource and binds no broker.
distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Pricing_Grpc>("pricing");

var basketService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Basket_Api>("basket")
    .WithReference(basketDatabase)
    .WithReference(distributedCache)
    .WithReference(rabbitMq)
    .WithEnvironment("MessageBroker__Host", rabbitMq.Resource.ConnectionStringExpression)
    .WaitFor(basketDatabase)
    .WaitFor(rabbitMq);

var inventoryService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Inventory_Api>("inventory")
    .WithReference(inventoryDatabase)
    .WithReference(rabbitMq)
    .WithEnvironment("MessageBroker__Host", rabbitMq.Resource.ConnectionStringExpression)
    .WaitFor(inventoryDatabase)
    .WaitFor(rabbitMq);

// Payment is reached only over internal REST from Ordering, never through the
// public gateway, so its handle is not captured for a downstream reference.
distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Payment_Api>("payment")
    .WithReference(paymentReadDatabase)
    .WithReference(paymentEventStore)
    .WaitFor(paymentReadDatabase)
    .WaitFor(paymentEventStore);

var orderingService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Ordering_Api>("ordering")
    .WithReference(orderingDatabase)
    .WithReference(rabbitMq)
    .WithEnvironment("MessageBroker__Host", rabbitMq.Resource.ConnectionStringExpression)
    .WaitFor(orderingDatabase)
    .WaitFor(rabbitMq);

var deliveryService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Delivery_Api>("delivery")
    .WithReference(deliveryDatabase)
    .WithReference(rabbitMq)
    .WithEnvironment("MessageBroker__Host", rabbitMq.Resource.ConnectionStringExpression)
    .WaitFor(deliveryDatabase)
    .WaitFor(rabbitMq);

var notificationService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Notification_Api>("notification")
    .WithReference(notificationsDatabase)
    .WithReference(distributedCache)
    .WithReference(rabbitMq)
    .WithEnvironment("MessageBroker__Host", rabbitMq.Resource.ConnectionStringExpression)
    .WaitFor(notificationsDatabase)
    .WaitFor(distributedCache)
    .WaitFor(rabbitMq);

var reportingService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Reporting_Api>("reporting")
    .WithReference(reportingWarehouse)
    .WithReference(rabbitMq)
    .WithEnvironment("MessageBroker__Host", rabbitMq.Resource.ConnectionStringExpression)
    .WaitFor(reportingWarehouse)
    .WaitFor(rabbitMq);

var customerSupportService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_CustomerSupport_Api>("customersupport")
    .WithReference(supportChatTranscripts)
    .WithReference(distributedCache)
    .WaitFor(supportChatTranscripts)
    .WaitFor(distributedCache);

var reviewsService = distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Reviews_Api>("reviews")
    .WithReference(reviewsDatabase)
    .WithReference(rabbitMq)
    .WithEnvironment("MessageBroker__Host", rabbitMq.Resource.ConnectionStringExpression)
    .WaitFor(reviewsDatabase)
    .WaitFor(rabbitMq);

// The gateway is the single public ingress. It needs the shared Redis key ring
// for the BFF cookie exchange and a reference to every downstream cluster so
// Aspire service discovery resolves them by name.
distributedApplicationBuilder
    .AddProject<Projects.FreshCart_Gateway_Yarp>("gateway")
    .WithReference(distributedCache)
    .WithReference(identityService)
    .WithReference(catalogService)
    .WithReference(basketService)
    .WithReference(orderingService)
    .WithReference(inventoryService)
    .WithReference(deliveryService)
    .WithReference(notificationService)
    .WithReference(reportingService)
    .WithReference(customerSupportService)
    .WithReference(reviewsService)
    .WaitFor(distributedCache)
    .WaitFor(identityService);

await distributedApplicationBuilder.Build().RunAsync().ConfigureAwait(false);
