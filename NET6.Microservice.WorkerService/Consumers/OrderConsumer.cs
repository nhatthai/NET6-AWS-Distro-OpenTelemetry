using System.Diagnostics;
using MassTransit;
using NET6.Microservice.Core.OpenTelemetry;
using NET6.Microservice.WorkerService.Services;
using OpenTelemetry.Context.Propagation;

namespace NET6.Microservice.WorkerService.Consumers
{
    public class OrderConsumer : IConsumer<Messages.Commands.Order>
    {
        private readonly ILogger<OrderConsumer> _logger;
        private readonly EmailService _emailService;
        private static readonly ActivitySource _activitySource = new ActivitySource("OrderConsumer");
        private static readonly TextMapPropagator Propagator = new TraceContextPropagator();

        public OrderConsumer(ILogger<OrderConsumer> logger, EmailService emailService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public Task Consume(ConsumeContext<Messages.Commands.Order> context)
        {
            var data = context.Message;
            var correlationId = data.CorrelationId;

            // set property for extracting Propagation context
            var pros = new Dictionary<string, object>();
            pros["traceparent"] = correlationId;

            // Extract the PropagationContext of order message
            var parentContext = Propagator.Extract(default, pros, OpenTelemetryActivity.ExtractTraceContextFromProperties);

            using var activity = _activitySource.StartActivity(
                "Order.Product Consumer", ActivityKind.Consumer, parentContext.ActivityContext);

            OpenTelemetryActivity.AddActivityTagsMessage(activity);

            _logger.LogInformation("Consume Order Message {CorrelationId} {OrderNumber}", correlationId, data.OrderNumber);

            try
            {
                // TODO: call service/task
                Task.Delay(2000);
                _emailService.SendEmail(correlationId, Guid.NewGuid(), "testing@domain.com", "Order: " + data.OrderNumber);
                activity?.SetStatus(ActivityStatusCode.Ok, "Consume a message and process successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to send Email. {CorrelationId} ", correlationId);
                activity?.SetStatus(ActivityStatusCode.Error, "Error occured when sending email in OrderConsumer");
            }

            _logger.LogInformation("Consumed Order Message {CorrelationId} {OrderNumber}", correlationId, data.OrderNumber);
            return Task.CompletedTask;
        }
    }
}