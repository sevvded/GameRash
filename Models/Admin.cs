using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameRash.Models 
{
    public class Admin
    {
        public int AdminID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }
    }


}
