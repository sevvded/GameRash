using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GameRash.Models
{
    public class Wishlist
    {
        public int WishlistID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

        public int GameID { get; set; }
        public Game Game { get; set; }

        public DateTime AddedDate { get; set; }
    }

}