using LibraHub.Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace LibraHub.Library.Infrastructure.Migrations
{
    [DbContext(typeof(LibraryDbContext))]
    partial class LibraryDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true)
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("LibraHub.BuildingBlocks.Inbox.ProcessedMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("Id");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("EventType");

                    b.Property<string>("MessageId")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("MessageId");

                    b.Property<DateTime>("ProcessedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("ProcessedAt");

                    b.HasKey("Id");

                    b.HasIndex("MessageId")
                        .IsUnique();

                    b.ToTable("processed_messages", (string)null);
                });

            modelBuilder.Entity("LibraHub.BuildingBlocks.Outbox.OutboxMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("Id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("CreatedAt");

                    b.Property<string>("Error")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)")
                        .HasColumnName("Error");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("EventType");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("Payload");

                    b.Property<DateTime?>("ProcessedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("ProcessedAt");

                    b.HasKey("Id");

                    b.HasIndex("ProcessedAt", "CreatedAt");

                    b.ToTable("outbox_messages", (string)null);
                });

            modelBuilder.Entity("LibraHub.Library.Domain.Books.BookSnapshot", b =>
                {
                    b.Property<Guid>("BookId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("book_id");

                    b.Property<string>("Authors")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("authors");

                    b.Property<int>("Availability")
                        .HasColumnType("integer")
                        .HasColumnName("availability");

                    b.Property<string>("CoverRef")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("cover_ref");

                    b.Property<string>("PriceLabel")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("price_label");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("title");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.HasKey("BookId");

                    b.HasIndex("Availability");

                    b.ToTable("book_snapshots", (string)null);
                });

            modelBuilder.Entity("LibraHub.Library.Domain.Entitlements.Entitlement", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime>("AcquiredAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("acquired_at");

                    b.Property<Guid>("BookId")
                        .HasColumnType("uuid")
                        .HasColumnName("book_id");

                    b.Property<Guid?>("OrderId")
                        .HasColumnType("uuid")
                        .HasColumnName("order_id");

                    b.Property<string>("RevocationReason")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("revocation_reason");

                    b.Property<DateTime?>("RevokedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("revoked_at");

                    b.Property<int>("Source")
                        .HasColumnType("integer")
                        .HasColumnName("source");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("AcquiredAt");

                    b.HasIndex("BookId");

                    b.HasIndex("Status");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "BookId")
                        .IsUnique();

                    b.ToTable("entitlements", (string)null);
                });

            modelBuilder.Entity("LibraHub.Library.Domain.Reading.ReadingProgress", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("BookId")
                        .HasColumnType("uuid")
                        .HasColumnName("book_id");

                    b.Property<string>("Format")
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)")
                        .HasColumnName("format");

                    b.Property<int?>("LastPage")
                        .HasColumnType("integer")
                        .HasColumnName("last_page");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_updated_at");

                    b.Property<decimal>("ProgressPercentage")
                        .HasPrecision(5, 2)
                        .HasColumnType("numeric(5,2)")
                        .HasColumnName("progress_percentage");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<int?>("Version")
                        .HasColumnType("integer")
                        .HasColumnName("version");

                    b.HasKey("Id");

                    b.HasIndex("BookId");

                    b.HasIndex("UserId");

                    b.HasIndex("UserId", "BookId", "Format", "Version")
                        .IsUnique();

                    b.ToTable("reading_progress", (string)null);
                });

            modelBuilder.Entity("LibraHub.Library.Infrastructure.Idempotency.IdempotencyKey", b =>
                {
                    b.Property<string>("Key")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("key");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("content_type");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<byte[]>("ResponseBody")
                        .IsRequired()
                        .HasColumnType("bytea")
                        .HasColumnName("response_body");

                    b.Property<int>("StatusCode")
                        .HasColumnType("integer")
                        .HasColumnName("status_code");

                    b.HasKey("Key");

                    b.HasIndex("CreatedAt");

                    b.ToTable("idempotency_keys", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
