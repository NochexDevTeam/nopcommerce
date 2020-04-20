using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Nop.Core;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Nochex.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.Nochex
{
    /// <summary>
    /// Nochex payment processor
    /// </summary>
    public class NochexPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private const string SUCCESS_ORDER_URL = "Plugins/PaymentNochex/SuccessOrder";
        private const string CANCEL_URL = "Plugins/PaymentNochex/CancelOrder";
        internal const string CALLBACK_URL = "Plugins/PaymentNochex/APCHandler";

        private readonly NochexPaymentSettings _NochexPaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWebHelper _webHelper;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        #endregion

        #region Ctor

        public NochexPaymentProcessor(NochexPaymentSettings NochexPaymentSettings,
            ILocalizationService localizationService,
            ILogger logger,
            IPaymentService paymentService,
            IHttpContextAccessor httpContextAccessor,
            ISettingService settingService,
            IShoppingCartService shoppingCartService,
            IWebHelper webHelper)
        {

            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _NochexPaymentSettings = NochexPaymentSettings;
            _localizationService = localizationService;
            _paymentService = paymentService;
            _settingService = settingService;
            _shoppingCartService = shoppingCartService;
            _webHelper = webHelper;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            try
            {

            var storeUrl = _webHelper.GetStoreLocation(false);
            var order = postProcessPaymentRequest.Order;
            var orderTotal = Math.Round(order.OrderTotal, 2);
                ///var orderDescription = GetOrderDescription(order);

                var post = new RemotePost { Url = "https://secure.nochex.com" };

                // Merchant Id
                post.Add("merchant_id", _NochexPaymentSettings.MerchantID);
                string orderDesc = "";
                string xmlCollection = "<items>";

                foreach (var item in postProcessPaymentRequest.Order.OrderItems)
                {
                    orderDesc += "" + item.Product.Name + " - " + item.Quantity.ToString() + " x " + item.PriceInclTax.ToString();
                    xmlCollection += "<item><id></id><name>" + item.Product.Name + "</name><description>" + item.Product.ShortDescription + "</description><quantity>" + item.Quantity.ToString() + "</quantity><price>" + item.PriceInclTax.ToString() + "</price></item>";
                }
                xmlCollection += "</items>";
                
                    // Urls
                post.Add("cancel_url", storeUrl + CANCEL_URL);
            post.Add("success_url", storeUrl + SUCCESS_ORDER_URL);
            post.Add("test_success_url", storeUrl + SUCCESS_ORDER_URL);
            post.Add("callback_url", storeUrl + CALLBACK_URL);

            // Order Details
            post.Add("order_id", order.CustomOrderNumber);
            post.Add("optional_1", order.OrderGuid.ToString());
            post.Add("amount", orderTotal.ToString("0.00"));

            // Billing Address
            var billingAddress = order.BillingAddress;
            post.Add("billing_fullname", billingAddress.FirstName + ", " + billingAddress.LastName);
            post.Add("billing_address", billingAddress.Address1 + ", " + billingAddress.Address2);
            post.Add("billing_city", billingAddress.City);
            post.Add("billing_postcode", billingAddress.ZipPostalCode);
            post.Add("email_address", billingAddress.Email);
            post.Add("customer_phone_number", billingAddress.PhoneNumber);

            // Shipping Address
 
                var shippingAddress = order.ShippingAddress;
                post.Add("delivery_fullname", shippingAddress.FirstName + ", " + shippingAddress.LastName);
                post.Add("delivery_address", shippingAddress.Address1 + ", " + shippingAddress.Address2);
                post.Add("delivery_city", billingAddress.City);
                post.Add("delivery_postcode", shippingAddress.ZipPostalCode);

                if (_NochexPaymentSettings.ProdInfoMode)
                {
                    post.Add("xml_item_collection", xmlCollection);
                    post.Add("description", "Order for:" + order.CustomOrderNumber);
                }
                else
                {
                    post.Add("description", orderDesc);
                }
                if (_NochexPaymentSettings.TransactMode)
            {
                post.Add("test_transaction", "100");
               }

                if (_NochexPaymentSettings.HideBillingDetails)
                {
                    post.Add("hide_billing_details", "true");
                }

                post.Post();

               // _httpContextAccessor.HttpContext.Response.Redirect(post);

            }

            catch (Exception ex)
            {
                _logger.InsertLog(LogLevel.Error, ex.Message, fullMessage: ex.ToString());
            }

        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country

           

            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _paymentService.CalculateAdditionalFee(cart,
                _NochexPaymentSettings.AdditionalFee, false);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentNochex/Configure";
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "Nochex";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new NochexPaymentSettings
            {
                DescriptionText = "Secure Payment by Debit / Credit Card."
            };
            _settingService.SaveSetting(settings);

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.AdditionalFee", "Additional fee");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.AdditionalFee.Hint", "The additional fee.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.AdditionalFeePercentage", "Additional fee. Use percentage");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.DescriptionText", "Description");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.DescriptionText.Hint", "Enter info that will be shown to customers during checkout");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.MerchantID", "Nochex Registered Email Address");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.MerchantID.Hint", "Nochex Registered Email Address / Merchant ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.PaymentMethodDescription", "Pay by cheque or money order");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.TransactMode", "Test Mode");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.TransactMode.Hint", "Specify transaction mode.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.HideBillingDetails", "Hide Billing Details");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.HideBillingDetails.Hint", "Hide Billing Details on the payment page.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.ProdInfoMode", "Detailed Product Information");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.ProdInfoMode.Hint", "Display Ordered Items on the payment page in a table-structured format.");
            
            ///_localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.ShippableProductRequired", "Shippable product required");
            ///_localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payment.Nochex.ShippableProductRequired.Hint", "An option indicating whether shippable products are required in order to display this payment method during checkout.");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<NochexPaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.AdditionalFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.AdditionalFeePercentage");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.AdditionalFeePercentage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.MerchantID");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.MerchantID.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.DescriptionText");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.DescriptionText.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.PaymentMethodDescription");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.ShippableProductRequired");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.ShippableProductRequired.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.TransactMode");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.TransactMode.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.HideBillingDetails");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.HideBillingDetails.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.ProdInfoMode");
            _localizationService.DeletePluginLocaleResource("Plugins.Payment.Nochex.ProdInfoMode.Hint");
                        
            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            get { return _localizationService.GetResource("Plugins.Payment.Nochex.PaymentMethodDescription"); }
        }

        #endregion

    }
}
 