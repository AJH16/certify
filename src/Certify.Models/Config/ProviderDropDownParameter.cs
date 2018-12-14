using System;
using System.Collections.Generic;
using System.Text;

namespace Certify.Models.Config
{
    public class ProviderDropDownParameter : ProviderParameter
    {
        private List<string> _options = new List<string>();
		public List<string> Options { get => this._options; }
    }
}
