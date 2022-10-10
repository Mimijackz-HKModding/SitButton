using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding;
using UnityEngine;
using InControl;

namespace SitButton
{
    public class SitActionset : PlayerActionSet
    {
        public PlayerAction sit;
        public SitActionset()
        {
            sit = CreatePlayerAction("Sit");

            sit.AddDefaultBinding(Key.DownArrow);
        }
    }
}
