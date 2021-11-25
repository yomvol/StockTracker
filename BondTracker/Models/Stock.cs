using System;
using System.Collections.Generic;
using System.Text;

namespace BondTracker.Models
{
    internal class Stock : Security
    {
        public const uint SecType = (int)SecTypes.Stock;
        public bool IsDivident { get; set; }
        public double DividentsPerShare { get; set; } // For year
        public DateTime NextDividentPayment { get; set; }

    }
}
