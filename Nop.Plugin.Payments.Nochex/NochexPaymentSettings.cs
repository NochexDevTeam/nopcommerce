using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Nochex
{
    /// <summary>
    /// Represents settings of "Check money order" payment plugin
    /// </summary>
    public class NochexPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a description text
        /// </summary>
        public string DescriptionText { get; set; }

        /// <summary>
        /// Gets or sets a description text
        /// </summary>
        public string MerchantID { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool TransactMode { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool HideBillingDetails { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool ProdInfoMode { get; set; }
        
    }
}
