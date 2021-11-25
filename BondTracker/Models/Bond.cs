using System;
using System.Collections.Generic;
using System.Text;

namespace BondTracker.Models
{
    class Bond : Security
    {
        public const uint SecType = (int)SecTypes.Bond;
        public const double NominalValue = 1000.0; // Roubles
        public double CouponValue { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime MaturityDate { get; set; } // date of repayment
        public DateTime NextCouponPayday { get; set; }
        //public uint NumberOfCouponsPaid { get; private set; } I have to get current date firstly
        public double YieldToMaturity { get; set; } // in percentages
        public int IntervalBetweenCoupons;

        public Bond()
        {
            NumberOfSharesInLot = 1;
        }
    }
}
