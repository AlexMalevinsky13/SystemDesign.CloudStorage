using Microsoft.EntityFrameworkCore;
using SystemDesign.CloudStorage.Domain.Models;

namespace SystemDesign.CloudStorage.Persistence;


/// <summary>
/// The PostgreSQL metadata database context.
/// </summary>
public sealed class CloudStorageDbContext(DbContextOptions<CloudStorageDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The users.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Refresh tokens.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Buckets.
    /// </summary>
    public DbSet<Bucket> Buckets => Set<Bucket>();

    /// <summary>
    /// The logical objects.
    /// </summary>
    public DbSet<StoredObject> Objects => Set<StoredObject>();

    /// <summary>
    /// The object versions.
    /// </summary>
    public DbSet<ObjectVersion> ObjectVersions => Set<ObjectVersion>();

    /// <summary>
    /// The custom metadata entries.
    /// </summary>
    public DbSet<ObjectMetadata> ObjectMetadata => Set<ObjectMetadata>();

    /// <summary>
    /// Blobs.
    /// </summary>
    public DbSet<PhysicalBlob> Blobs => Set<PhysicalBlob>();

    /// <summary>
    /// Multipart uploads.
    /// </summary>
    public DbSet<MultipartUpload> MultipartUploads => Set<MultipartUpload>();

    /// <summary>
    /// Multipart parts.
    /// </summary>
    public DbSet<MultipartPart> MultipartParts => Set<MultipartPart>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureModel(modelBuilder);
    }

    /// <summary>
    /// Configures the metadata model for both the runtime context and migration snapshots.
    /// </summary>
    internal static void ConfigureModel(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Login).HasMaxLength(64);
            e.Property(x => x.Email).HasMaxLength(320);
            e.HasIndex(x => x.Login).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Bucket>(e =>
        {
            e.ToTable("buckets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(63);
            e.HasIndex(x => new { x.OwnerId, x.Name }).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<StoredObject>(e =>
        {
            e.ToTable("objects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(1024);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => new { x.BucketId, x.Key }).IsUnique();
            e.HasOne<Bucket>().WithMany().HasForeignKey(x => x.BucketId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ObjectVersion>(e =>
        {
            e.ToTable("object_versions");
            e.HasKey(x => x.Id);
            e.Property(x => x.ETag).HasMaxLength(70);
            e.Property(x => x.Sha256).HasMaxLength(64);
            e.HasIndex(x => new { x.StoredObjectId, x.Sequence }).IsUnique();
            e.HasOne<StoredObject>()
                .WithMany(x => x.Versions)
                .HasForeignKey(x => x.StoredObjectId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<PhysicalBlob>().WithMany().HasForeignKey(x => x.BlobId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ObjectMetadata>(e =>
        {
            e.ToTable("object_metadata");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(128);
            e.Property(x => x.Value).HasMaxLength(2048);
            e.HasIndex(x => new { x.ObjectVersionId, x.Key }).IsUnique();
            e.HasOne<ObjectVersion>()
                .WithMany(x => x.Metadata)
                .HasForeignKey(x => x.ObjectVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PhysicalBlob>(e =>
        {
            e.ToTable("physical_blobs");
            e.HasKey(x => x.Id);
            e.Property(x => x.RelativePath).HasMaxLength(160);
            e.Property(x => x.Sha256).HasMaxLength(64);
            e.HasIndex(x => x.RelativePath).IsUnique();
        });

        b.Entity<MultipartUpload>(e =>
        {
            e.ToTable("multipart_uploads");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(1024);
            e.HasIndex(x => new { x.Status, x.ExpiresAtUtc });
            e.HasOne<Bucket>().WithMany().HasForeignKey(x => x.BucketId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MultipartPart>(e =>
        {
            e.ToTable("multipart_parts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MultipartUploadId, x.PartNumber }).IsUnique();
            e.HasOne<MultipartUpload>()
                .WithMany(x => x.Parts)
                .HasForeignKey(x => x.MultipartUploadId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
