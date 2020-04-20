using System.Collections.Generic;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Nochex.Models
{
    public class ConfigurationModel : BaseNopModel, ILocalizedModel<ConfigurationModel.ConfigurationLocalizedModel>
    {
        public ConfigurationModel()
        {
            Locales = new List<ConfigurationLocalizedModel>();
        }

        public int ActiveStoreScopeConfiguration { get; set; }
        
        [NopResourceDisplayName("Plugins.Payment.Nochex.DescriptionText")]
        public string DescriptionText { get; set; }
        public bool DescriptionText_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.Nochex.MerchantID")]
        public string MerchantID { get; set; }
        public bool MerchantID_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.Nochex.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.Nochex.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.Nochex.ShippableProductRequired")]
        public bool ShippableProductRequired { get; set; }
        public bool ShippableProductRequired_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.Nochex.TransactMode")]
        public bool TransactMode { get; set; }
        public bool TransactMode_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.Nochex.HideBillingDetails")]
        public bool HideBillingDetails { get; set; }
        public bool HideBillingDetails_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.Nochex.ProdInfoMode")]
        public bool ProdInfoMode { get; set; }
        public bool ProdInfoMode_OverrideForStore { get; set; }
        public IList<ConfigurationLocalizedModel> Locales { get; set; }
        
        #region Nested class

        public partial class ConfigurationLocalizedModel : ILocalizedLocaleModel
        {
            public int LanguageId { get; set; }
            
            [NopResourceDisplayName("Plugins.Payment.Nochex.DescriptionText")]
            public string DescriptionText { get; set; }
            
        }

        #endregion

    }
}