namespace Plugin.Sample.Commerce.Payment.Deferred
{
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using Plugin.Sample.Commerce.Payment.Deferred.Pipelines.Blocks;
    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Plugin.Orders;
    using Sitecore.Commerce.Plugin.Payments;
    using Sitecore.Framework.Configuration;
    using Sitecore.Framework.Pipelines.Definitions.Extensions;

    /// <summary>
    /// The configure sitecore class.
    /// </summary>
    public class ConfigureSitecore : IConfigureSitecore
    {
        /// <summary>
        /// The configure services.
        /// </summary>
        /// <param name="services">
        /// The services.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

             services.Sitecore().Pipelines(config => config
                .ConfigurePipeline<IConfigureServiceApiPipeline>(c => c.Add<ConfigureServiceApiBlock>())
                .ConfigurePipeline<ISettleSalesActivityPipeline>(c => c.Add<SettleDeferredPaymentBlock>().Before<MoveAndPersistSalesActivityBlock>())
                .ConfigurePipeline<IRefundPaymentsPipeline>(c => c.Add<RefundDeferredPaymentBlock>().Before<PersistOrderBlock>())
                );

            services.RegisterAllCommands(assembly);
        }
    }
}