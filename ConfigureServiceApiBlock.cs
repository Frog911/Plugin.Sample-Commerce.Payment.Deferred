namespace Plugin.Sample.Commerce.Payment.Deferred
{
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.OData.Builder;
    using Plugin.Sample.Commerce.Payment.Deferred.Components;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Core.Commands;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Pipelines;

    [PipelineDisplayName(nameof(ConfigureServiceApiBlock))]
    public class ConfigureServiceApiBlock : PipelineBlock<ODataConventionModelBuilder, ODataConventionModelBuilder, CommercePipelineExecutionContext>
    {

        public override Task<ODataConventionModelBuilder> Run(ODataConventionModelBuilder modelBuilder, CommercePipelineExecutionContext context)
        {
            Condition.Requires(modelBuilder).IsNotNull($"{this.Name}: The argument cannot be null.");

            modelBuilder.AddEntityType(typeof(DeferredPaymentComponent));

            ActionConfiguration addDeferredPaymentConfiguration = modelBuilder.Action("AddDeferredPayment");
            addDeferredPaymentConfiguration.Parameter<string>("cartId");
            addDeferredPaymentConfiguration.Parameter<DeferredPaymentComponent>("payment");
            addDeferredPaymentConfiguration.ReturnsFromEntitySet<CommerceCommand>("Commands");

            return Task.FromResult(modelBuilder);
        }
    }
}
