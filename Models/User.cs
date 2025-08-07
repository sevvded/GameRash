using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRash.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public Admin Admin { get; set; }
        public Developer Developer { get; set; }
        public ICollection<Library> Libraries { get; set; }
        public ICollection<Wishlist> Wishlists { get; set; } 
        public ICollection<Purchase> Purchases { get; set; }
        public ICollection<GameReview> GameReviews { get; set; }
    }

}