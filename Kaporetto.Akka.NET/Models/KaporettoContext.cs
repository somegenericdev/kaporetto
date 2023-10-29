using System;
using System.Collections.Generic;
using Kaporetto.Models;
using Microsoft.EntityFrameworkCore;
using File = Kaporetto.Models.File;

namespace Kaporetto.Akka.NET;

public partial class KaporettoContext : DbContext
{
    public KaporettoContext()
    {
    }

    public KaporettoContext(DbContextOptions<KaporettoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<File> Files { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<FileContent> FileContents { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // modelBuilder.Entity<Post>()
        //     .HasMany(e => e.files)
        //     .WithOne(e => e.Post)
        //     .HasForeignKey(e => e.post_reference)
        //     .IsRequired(false);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
