using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace GameRash.Models
{
    public class Developer
    {
        public int DeveloperID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

        public string StudioName { get; set; }
        public string Bio { get; set; }

        public ICollection<Game> Games { get; set; }
    }

}