using Plugin.Sample.Commerce.Payment.Deferred.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Sample.Commerce.Payment.Deferred.Pipelines.Blocks
{
    [PipelineDisplayName(nameof(RefundDeferredPaymentBlock))]
    public class RefundDeferredPaymentBlock : PipelineBlock<OrderPaymentsArgument, OrderPaymentsArgument, CommercePipelineExecutionContext>
    {

        protected CommerceCommander Commander { get; set; }

        public RefundDeferredPaymentBlock(CommerceCommander commander)
            : base(null)
        {

            this.Commander = commander;

        }

        public async override Task<OrderPaymentsArgument> Run(OrderPaymentsArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{this.Name} The arg can not be null");
            Condition.Requires(arg.Order).IsNotNull($"{this.Name} The order can not be null");
            Condition.Requires(arg.Payments).IsNotNull($"{this.Name} The payments can not be null");

            var order = arg.Order;
            if (!order.HasComponent<DeferredPaymentComponent>())
            {
                return arg;
            }

            if (!order.Status.Equals(context.GetPolicy<KnownOrderStatusPolicy>().Completed, StringComparison.OrdinalIgnoreCase))
            {
                var invalidOrderStateMessage = $"{this.Name}: Expected order in '{context.GetPolicy<KnownOrderStatusPolicy>().Completed}' status but order was in '{order.Status}' status";
                await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().ValidationError,
                        "InvalidOrderState",
                        new object[] { context.GetPolicy<KnownOrderStatusPolicy>().Completed, order.Status },
                        invalidOrderStateMessage);
                return null;
            }

            var existingPayment = order.GetComponent<DeferredPaymentComponent>();
            var paymentToRefund = arg.Payments.FirstOrDefault(p => p.Id.Equals(existingPayment.Id, StringComparison.OrdinalIgnoreCase)) as DeferredPaymentComponent;
            if (paymentToRefund == null)
            {
                return arg;
            }

            if (existingPayment.Amount.Amount < paymentToRefund.Amount.Amount)
            {
                await context.CommerceContext.AddMessage(
                    context.GetPolicy<KnownResultCodes>().Error,
                    "IllegalRefundOperation",
                    new object[] { order.Id, existingPayment.Id },
                    "Order Deferred Payment amount is less than refund amount");
                return null;
            }

            // Perform logic to reverse the actual payment

            if (existingPayment.Amount.Amount == paymentToRefund.Amount.Amount)
            {
                // Remove the existingPayment from the order since the entire amount is refunded
                order.Components.Remove(existingPayment);
            }
            else
            {
                // Reduce the existing existingPayment in the order
                existingPayment.Amount.Amount -= paymentToRefund.Amount.Amount;
            }

            await this.GenerateSalesActivity(order, existingPayment, paymentToRefund, context);

            return arg;
        }

        private async Task GenerateSalesActivity(Order order,
            PaymentComponent existingPayment,
            PaymentComponent paymentToRefund,
            CommercePipelineExecutionContext context)
        {
            var salesActivity = new SalesActivity
            {
                Id = $"{CommerceEntity.IdPrefix<SalesActivity>()}{Guid.NewGuid():N}",
                ActivityAmount = new Money(existingPayment.Amount.CurrencyCode, paymentToRefund.Amount.Amount * -1),
                Customer = new EntityReference
                {
                    EntityTarget = order.Components.OfType<ContactComponent>().FirstOrDefault()?.CustomerId
                },
                Order = new EntityReference
                {
                    EntityTarget = order.Id
                },
                Name = "Deferred Payment Refund",
                PaymentStatus = context.GetPolicy<KnownSalesActivityStatusesPolicy>().Completed
            };

            // Add the new sales activity to the OrderSalesActivities list
            salesActivity.SetComponent(new ListMembershipsComponent
            {
                Memberships = new List<string>
                    {
                        CommerceEntity.ListName<SalesActivity>(),
                        context.GetPolicy<KnownOrderListsPolicy>().SalesCredits,
                        string.Format(context.GetPolicy<KnownOrderListsPolicy>().OrderSalesActivities, order.FriendlyId)
                    }
            });

            if (existingPayment.Amount.Amount != paymentToRefund.Amount.Amount)
            {
                salesActivity.SetComponent(existingPayment);
            }

            var salesActivities = order.SalesActivity.ToList();
            salesActivities.Add(new EntityReference { EntityTarget = salesActivity.Id });
            order.SalesActivity = salesActivities;

            await Commander.PersistEntity(context.CommerceContext, salesActivity);
        }
    }
}
