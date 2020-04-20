using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Nochex.Models;
using Nop.Core;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System.Net.Mail;

namespace Nop.Plugin.Payments.Nochex.Controllers
{
    public class PaymentNochexController : BaseController
    {
        /*BasePaymentController*/
        #region Fields

        private readonly NochexPaymentSettings _settings;

        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        //private readonly IStoreService _storeService;
        private readonly IWebHelper _webHelper;
        private readonly ILogger _logger;

        private const string NOCHEX_APC_URL = "https://www.nochex.com/apcnet/apc.aspx";
        #endregion

        #region Ctor

        public PaymentNochexController(ILanguageService languageService,
            ILocalizationService localizationService,
            INotificationService notificationService,
              IOrderService orderService,
              IOrderProcessingService orderProcessingService,
              IWorkContext workContext,
              IStoreService storeService,
               IWebHelper webHelper,
            IPermissionService permissionService,
            ISettingService settingService,
            ILogger logger,
      NochexPaymentSettings settings,
            IStoreContext storeContext)
        {
            _settings = settings;
            _logger = logger;
            _workContext = workContext;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _languageService = languageService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var NochexPaymentSettings = _settingService.LoadSetting<NochexPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                DescriptionText = NochexPaymentSettings.DescriptionText
            };

            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.DescriptionText = _localizationService
                    .GetLocalizedSetting(NochexPaymentSettings, x => x.DescriptionText, languageId, 0, false, false);
            });
            model.MerchantID = NochexPaymentSettings.MerchantID;
            model.AdditionalFee = NochexPaymentSettings.AdditionalFee;
            model.TransactMode = NochexPaymentSettings.TransactMode;
            model.HideBillingDetails = NochexPaymentSettings.HideBillingDetails;
            model.ProdInfoMode = NochexPaymentSettings.ProdInfoMode;
            
            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.DescriptionText_OverrideForStore = _settingService.SettingExists(NochexPaymentSettings, x => x.DescriptionText, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(NochexPaymentSettings, x => x.AdditionalFee, storeScope);
            }

            return View("~/Plugins/Payments.Nochex/Views/Configure.cshtml", model);
        }
        
        [HttpPost]
        [AuthorizeAdmin]
        [AdminAntiForgery]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var NochexPaymentSettings = _settingService.LoadSetting<NochexPaymentSettings>(storeScope);

            //save settings
            NochexPaymentSettings.DescriptionText = model.DescriptionText;
            NochexPaymentSettings.MerchantID = model.MerchantID;
            NochexPaymentSettings.AdditionalFee = model.AdditionalFee;
            NochexPaymentSettings.TransactMode = model.TransactMode;
            NochexPaymentSettings.HideBillingDetails = model.HideBillingDetails;
            NochexPaymentSettings.ProdInfoMode = model.ProdInfoMode;

            
            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(NochexPaymentSettings, x => x.DescriptionText, model.DescriptionText_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(NochexPaymentSettings, x => x.MerchantID, model.MerchantID_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(NochexPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(NochexPaymentSettings, x => x.TransactMode, model.TransactMode_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(NochexPaymentSettings, x => x.HideBillingDetails, model.HideBillingDetails_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(NochexPaymentSettings, x => x.ProdInfoMode, model.ProdInfoMode_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            //localization. no multi-store support for localization yet.
            foreach (var localized in model.Locales)
            {
                _localizationService.SaveLocalizedSetting(NochexPaymentSettings,
                    x => x.DescriptionText, localized.LanguageId, localized.DescriptionText);
            }

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }


        public IActionResult APCHandler()
        {


            byte[] parameters;

            using (var streams = new MemoryStream())
            {
                Request.Body.CopyTo(streams);
                parameters = streams.ToArray();
            }

            var postdetails = Encoding.ASCII.GetString(parameters);

            var dePostDets = WebUtility.UrlDecode(postdetails);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in dePostDets.Split('&'))
            {
                var line = l.Trim();
                var equalPox = line.IndexOf('=');
                if (equalPox >= 0)
                    values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
            }


            // Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create("https://www.nochex.com/apcnet/apc.aspx");
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.


            byte[] byteArray = Encoding.UTF8.GetBytes(postdetails);
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();

            // Get the response.
            WebResponse response = request.GetResponse();

            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();

            var showVal = values.TryGetValue("custom", out var orderGuid);
            var showtransId = values.TryGetValue("transaction_id", out var transactionId);
            var showStatus = values.TryGetValue("status", out var status);
            
            var order = _orderService.GetOrderByGuid(new Guid(orderGuid));
            if (order == null)
            {
                _logger.InsertLog(LogLevel.Warning, "Nochex APC: Order not found: " + orderGuid, responseFromServer);
            }

            var orderNote = new OrderNote
            {
                Note = "Nochex APC Message Received: \r\n\r\n" + responseFromServer,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            };

            order.OrderNotes.Add(orderNote);
            _orderService.UpdateOrder(order);
            order.AuthorizationTransactionId = transactionId;
            _orderService.UpdateOrder(order);
            _orderProcessingService.MarkOrderAsPaid(order);

            return Content(responseFromServer);
            //ApplyApcCallback(postdetails);
            //return Content(string.Empty);
        }
       /* private void ApplyApcCallback(string requestMessage)
        {
            var apcMessage = ParseApcMessage(requestMessage);
            if (apcMessage == null)
            {
                _logger.InsertLog(LogLevel.Warning, "Nochex APC Message is invalid", requestMessage);
                return;
            }

            var orderGuid = apcMessage["custom"];
            var transactionId = apcMessage["transaction_id"];
            var status = apcMessage["status"];

            if (orderGuid == null || transactionId == null || status == null)
            {
                _logger.InsertLog(LogLevel.Warning, "Nochex APC: Missing required data", requestMessage);
                return;
            }


            if (!_orderProcessingService.CanMarkOrderAsPaid(order))
            {
                _logger.InsertLog(LogLevel.Warning, "Nochex APC: Cannot mark order as paid: " + orderGuid, requestMessage);
                return;
            }

        }
        
        private static string BuildFriendlyApcMessage(NameValueCollection apcMessage)
        {
            return string.Join("\r\n", apcMessage.Keys.Cast<string>().Select(i => i + " = " + apcMessage[i]));
        }
 private static NameValueCollection ParseApcMessage(string message)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            /*var request = (HttpWebRequest)WebRequest.Create("https://www.nochex.com/apcnet/apc.aspx");*
            WebRequest request = WebRequest.Create("https://www.nochex.com/apcnet/apc.aspx");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = message.Length;

            var requestStream = request.GetRequestStream();
            requestStream.Write(Encoding.UTF8.GetBytes(message), 0, message.Length);
            requestStream.Close();

            var responseStream = request.GetResponse().GetResponseStream();
            if (responseStream == null)
            {
                // No response
                return null;
            }

            using (var reader = new StreamReader(responseStream))
            {
                var responseText = HttpUtility.UrlDecode(reader.ReadToEnd());
                if (!string.Equals(responseText, "AUTHORISED", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Not authorised
                    return null;
                }
            }

            return HttpUtility.ParseQueryString(message);
        }*/


        #endregion

        public ActionResult SuccessOrder()
        {
            var lastOrder = _orderService
              .SearchOrders(storeId: _storeContext.CurrentStore.Id, customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
              .FirstOrDefault();

            if (lastOrder != null)
            {
                // Redirect to order details
                return RedirectToRoute("OrderDetails", new { orderId = lastOrder.Id });
            }
            else
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            //Redirect to the home page
        }

        public IActionResult CancelOrder()
        {
            var order = _orderService.SearchOrders(_storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1).FirstOrDefault();

            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("Homepage");
        }
    }
}