using System;
using System.Collections.Generic;
using System.Text;

namespace BondTracker.Models
{
    abstract class Security
    {
        public uint ID { get; set; } // !!! Protected or Public?
        public string Name { get; set; }
        public double Price { get; set; }
        public double Change { get; set; } // in percentages?
        public uint NumberOfSharesInLot { get; set; } // Is exchange specific?

    }
}

enum SecTypes
{
    Stock = 1,
    Bond
}