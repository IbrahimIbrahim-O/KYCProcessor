﻿using KYCProcessor.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet for User entity
        public DbSet<User> Users { get; set; }

        public DbSet<KycForm> KycForms { get; set; }

        public DbSet<UserCredit> UserCredits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserCredit>()
                .Property(u => u.Amount)
                .HasPrecision(18, 2); 
        }
    }
}
