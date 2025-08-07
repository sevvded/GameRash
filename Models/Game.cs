using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GameRash.Models
{
    public class Game
    {
        [Key]
        public int GameID { get; set; }

        public int DeveloperID { get; set; }
        public Developer? Developer { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CoverImage { get; set; } = string.Empty;

        public ICollection<Library> Libraries { get; set; } = new List<Library>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public ICollection<GameReview> GameReviews { get; set; } = new List<GameReview>();
    }

}