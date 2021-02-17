using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Nochex.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            /// APC
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Nochex.APCHandler", "Plugins/PaymentNochex/APCHandler",
                new { controller = "PaymentNochex", action = "APCHandler" });


            /// CancelOrder
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Nochex.CancelOrder", "Plugins/PaymentNochex/CancelOrder",
                new { controller = "PaymentNochex", action = "CancelOrder" });

            /// CancelOrder
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Nochex.SuccessOrder", "Plugins/PaymentNochex/SuccessOrder",
                new { controller = "PaymentNochex", action = "SuccessOrder" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => -1;
    }
}