using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GameRash.Models
{
    public class Purchase
    {
        public int PurchaseID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

        public int GameID { get; set; }
        public Game Game { get; set; }

        public DateTime PurchaseDate { get; set; }

        public Payment Payment { get; set; }

        public ICollection<Payment> Payments { get; set; }
    }



}
