using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SystemDesign.CloudStorage.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "physical_blobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RelativePath = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_physical_blobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "buckets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buckets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_buckets_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "multipart_uploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BucketId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_multipart_uploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_multipart_uploads_buckets_BucketId",
                        column: x => x.BucketId,
                        principalTable: "buckets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "objects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BucketId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_objects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_objects_buckets_BucketId",
                        column: x => x.BucketId,
                        principalTable: "buckets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "multipart_parts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MultipartUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<int>(type: "integer", nullable: false),
                    RelativePath = table.Column<string>(type: "text", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    ETag = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_multipart_parts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_multipart_parts_multipart_uploads_MultipartUploadId",
                        column: x => x.MultipartUploadId,
                        principalTable: "multipart_uploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "object_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredObjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleteMarker = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    ContentLength = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ETag = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BlobId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_object_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_object_versions_objects_StoredObjectId",
                        column: x => x.StoredObjectId,
                        principalTable: "objects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_object_versions_physical_blobs_BlobId",
                        column: x => x.BlobId,
                        principalTable: "physical_blobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "object_metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_object_metadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_object_metadata_object_versions_ObjectVersionId",
                        column: x => x.ObjectVersionId,
                        principalTable: "object_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_buckets_OwnerId_Name",
                table: "buckets",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_multipart_parts_MultipartUploadId_PartNumber",
                table: "multipart_parts",
                columns: new[] { "MultipartUploadId", "PartNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_multipart_uploads_BucketId",
                table: "multipart_uploads",
                column: "BucketId");

            migrationBuilder.CreateIndex(
                name: "IX_multipart_uploads_Status_ExpiresAtUtc",
                table: "multipart_uploads",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_object_metadata_ObjectVersionId_Key",
                table: "object_metadata",
                columns: new[] { "ObjectVersionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_object_versions_BlobId",
                table: "object_versions",
                column: "BlobId");

            migrationBuilder.CreateIndex(
                name: "IX_object_versions_StoredObjectId_Sequence",
                table: "object_versions",
                columns: new[] { "StoredObjectId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_objects_BucketId_Key",
                table: "objects",
                columns: new[] { "BucketId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_physical_blobs_RelativePath",
                table: "physical_blobs",
                column: "RelativePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId_ExpiresAtUtc",
                table: "refresh_tokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Login",
                table: "users",
                column: "Login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "multipart_parts");

            migrationBuilder.DropTable(
                name: "object_metadata");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "multipart_uploads");

            migrationBuilder.DropTable(
                name: "object_versions");

            migrationBuilder.DropTable(
                name: "objects");

            migrationBuilder.DropTable(
                name: "physical_blobs");

            migrationBuilder.DropTable(
                name: "buckets");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
