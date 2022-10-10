using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InControl;
using Modding.Converters;

namespace SitButton
{
    public class SitButtonSettings
    {
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public SitActionset sitAction = new SitActionset();

        public bool hideHUD = false;
        //public bool hidePrompt = false;
    }
}
