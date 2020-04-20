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
        /// <param name="routeBuilder">Route builder</param>
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            /// APC
            routeBuilder.MapRoute("Plugin.Payments.Nochex.APCHandler", "Plugins/PaymentNochex/APCHandler",
                  new { controller = "PaymentNochex", action = "APCHandler" });

            /// CancelOrder
            routeBuilder.MapRoute("Plugin.Payments.Nochex.CancelOrder", "Plugins/PaymentNochex/CancelOrder",
                  new { controller = "PaymentNochex", action = "CancelOrder" });
            /// CancelOrder
            routeBuilder.MapRoute("Plugin.Payments.Nochex.SuccessOrder", "Plugins/PaymentNochex/SuccessOrder",
                  new { controller = "PaymentNochex", action = "SuccessOrder" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => -1;
    }
}