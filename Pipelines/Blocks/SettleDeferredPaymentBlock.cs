using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plugin.Sample.Commerce.Payment.Deferred.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.Commerce.Payment.Deferred.Pipelines.Blocks
{
    [PipelineDisplayName(nameof(SettleDeferredPaymentBlock))]
    public class SettleDeferredPaymentBlock : PipelineBlock<SalesActivity, SalesActivity, CommercePipelineExecutionContext>
    {
        protected CommerceCommander Commander { get; set; }

        public SettleDeferredPaymentBlock(CommerceCommander commander) : base(null)
        {
            this.Commander = commander;
        }

        public override Task<SalesActivity> Run(SalesActivity arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{this.Name}: The order cannot be null.");

            var salesActivity = arg;
            var knownSalesActivityStatuses = context.GetPolicy<KnownSalesActivityStatusesPolicy>();
            if (!salesActivity.HasComponent<DeferredPaymentComponent>()
                || !salesActivity.PaymentStatus.Equals(knownSalesActivityStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(salesActivity);
            }

            var payment = salesActivity.GetComponent<DeferredPaymentComponent>();
            context.Logger.LogInformation($"{this.Name} - Payment succeeded: {payment.Id}");
            salesActivity.PaymentStatus = knownSalesActivityStatuses.Settled;

            return Task.FromResult(salesActivity);
        }
    }
}
