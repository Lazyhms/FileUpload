using FileUpload.Entity;
using Microsoft.EntityFrameworkCore;

namespace FileUpload;


public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options)
        : base(options)
    {
    }

    public DbSet<FileDo> File { get; set; }
}
