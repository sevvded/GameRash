using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRash.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
                public Admin? Admin { get; set; }
        public Developer? Developer { get; set; }
        public ICollection<Library> Libraries { get; set; } = new List<Library>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public ICollection<GameReview> GameReviews { get; set; } = new List<GameReview>();
    }

}