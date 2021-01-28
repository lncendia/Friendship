using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Friendship
{
    class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public string City { get; set; }
        public sbyte Age { get; set; }
        public string AboutMe { get; set; }
        public sbyte Sex { get; set; }
        public string PhotoID { get; set; }
        public string FindFf { get; set; }
        public sbyte findSex;
        public bool IsDonate { get; set; } = false;
        public bool IsRegistered { get; set; } = false;
        public enum State { main, city, age,sex, photo, aboutmy,find,selectSex,wait }
        public State state;
    }
}
