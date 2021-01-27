using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Friendship
{
    class DB: DbContext
    {
        public DbSet<User> Users { get; set; }

        public DB()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=users.sqlite");
        }
    }
}
