using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GameRash.Models
{
    public class GameReview
    {
        public int ReviewID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

        public int GameID { get; set; }
        public Game Game { get; set; }

        public int Rating { get; set; }
    }


}