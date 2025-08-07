using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GameRash.Models
{
    public class Payment
    {
        public int PaymentID { get; set; }

        public int PurchaseID { get; set; }
        public Purchase Purchase { get; set; }

        public string PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; }
    }

}