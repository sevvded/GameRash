using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace GameRash.Models
{
    public class Developer
    {
        [Key]
        public int DeveloperID { get; set; }

        public int UserID { get; set; }
        public User? User { get; set; }

        public string StudioName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;

        public ICollection<Game> Games { get; set; } = new List<Game>();
    }

}