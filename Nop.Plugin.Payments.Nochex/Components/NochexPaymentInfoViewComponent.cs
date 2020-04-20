using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.Nochex.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Nochex.Components
{
    [ViewComponent(Name = "Nochex")]
    public class NochexViewComponent : NopViewComponent
    {
        private readonly NochexPaymentSettings _NochexPaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        public NochexViewComponent(NochexPaymentSettings NochexPaymentSettings,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _NochexPaymentSettings = NochexPaymentSettings;
            _localizationService = localizationService;
            _storeContext = storeContext;
            _workContext = workContext;
        }

        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel
            {
                DescriptionText = _localizationService.GetLocalizedSetting(_NochexPaymentSettings,
                    x => x.DescriptionText, _workContext.WorkingLanguage.Id, _storeContext.CurrentStore.Id)
            };

            return View("~/Plugins/Payments.Nochex/Views/PaymentInfo.cshtml", model);
        }
    }
}