using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Plugin.Sample.Commerce.Payment.Deferred.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Payments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.OData;

namespace Plugin.Sample.Commerce.Payment.Deferred.Controllers
{
    public class CommandsController : CommerceController
    {
        public CommandsController(IServiceProvider serviceProvider,
            CommerceEnvironment globalEnvironment)
            : base(serviceProvider, globalEnvironment)
        {
        }

        [HttpPut]
        [Route("AddDeferredPayment()")]
        public async Task<IActionResult> AddDeferredPayment([FromBody] ODataActionParameters value)
        {
            if (!this.ModelState.IsValid || value == null)
            {
                return new BadRequestObjectResult(this.ModelState);
            }

            if (!value.ContainsKey("cartId") ||
                (string.IsNullOrEmpty(value["cartId"]?.ToString()) ||
                !value.ContainsKey("payment")) ||
                string.IsNullOrEmpty(value["payment"]?.ToString()))
            {
                return new BadRequestObjectResult(value);
            }

            string cartId = value["cartId"].ToString();

            var paymentComponent = JsonConvert.DeserializeObject<DeferredPaymentComponent>(value["payment"].ToString());
            var command = this.Command<AddPaymentsCommand>();
            await command.Process(this.CurrentContext, cartId, new List<PaymentComponent> { paymentComponent });

            return new ObjectResult(command);
        }
    }
}
