using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace Certify.Models.Config
{
    public class ProviderPasswordParameter : ProviderParameter
    {
        public SecureString SecureValue { get; set; }

        public override bool IsPassword => true;

        public override string Value
        {
            get => new System.Net.NetworkCredential(string.Empty, this.SecureValue).Password;
            set => this.SecureValue = new System.Net.NetworkCredential(string.Empty, value).SecurePassword;
        }
    }
}
