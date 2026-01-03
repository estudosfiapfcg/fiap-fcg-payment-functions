using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCG.Payment.Functions.Unit.Test.Helpers
{
    public sealed class TestFunctionContext : FunctionContext
    {
        private IServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();

        public override string InvocationId { get; } = Guid.NewGuid().ToString();
        public override string FunctionId { get; } = "ProcessarComprasFunction";

        public override IServiceProvider InstanceServices
        {
            get => _serviceProvider;
            set => _serviceProvider = value;
        }
        
        public override TraceContext TraceContext => null!;
        public override BindingContext BindingContext => null!;
        public override RetryContext RetryContext => null!;
        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
        public override FunctionDefinition FunctionDefinition => null!;

        public override IInvocationFeatures Features => throw new NotImplementedException();
    }
}
