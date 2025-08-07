using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GameRash.Models
{
    public class Game
    {
        public int GameID { get; set; }

        public int DeveloperID { get; set; }
        public Developer Developer { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverImage { get; set; }

        public ICollection<Library> Libraries { get; set; }
        public ICollection<Wishlist> Wishlists { get; set; }
        public ICollection<Purchase> Purchases { get; set; }
        public ICollection<GameReview> GameReviews { get; set; }
    }

}