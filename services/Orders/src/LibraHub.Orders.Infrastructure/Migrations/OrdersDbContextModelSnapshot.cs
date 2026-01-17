using LibraHub.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace LibraHub.Orders.Infrastructure.Migrations
{
    [DbContext(typeof(OrdersDbContext))]
    internal partial class OrdersDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("LibraHub.BuildingBlocks.Inbox.ProcessedMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("MessageId")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime>("ProcessedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("MessageId")
                        .IsUnique();

                    b.ToTable("processed_messages", (string)null);
                });

            modelBuilder.Entity("LibraHub.BuildingBlocks.Outbox.OutboxMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Error")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("ProcessedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ProcessedAt", "CreatedAt");

                    b.ToTable("outbox_messages", (string)null);
                });

            modelBuilder.Entity("LibraHub.Orders.Domain.Orders.Order", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("CancellationReason")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("cancellation_reason");

                    b.Property<DateTime?>("CancelledAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("cancelled_at");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("Currency")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)")
                        .HasColumnName("currency");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("Status");

                    b.HasIndex("UserId");

                    b.ToTable("orders", (string)null);
                });

            modelBuilder.Entity("LibraHub.Orders.Domain.Orders.OrderItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("BookId")
                        .HasColumnType("uuid")
                        .HasColumnName("book_id");

                    b.Property<string>("BookTitle")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("book_title");

                    b.Property<decimal?>("DiscountAmount")
                        .HasPrecision(18, 2)
                        .HasColumnType("numeric(18,2)")
                        .HasColumnName("discount_amount");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uuid")
                        .HasColumnName("order_id");

                    b.Property<Guid?>("PromotionId")
                        .HasColumnType("uuid")
                        .HasColumnName("promotion_id");

                    b.Property<string>("PromotionName")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("promotion_name");

                    b.Property<decimal>("VatRate")
                        .HasPrecision(5, 2)
                        .HasColumnType("numeric(5,2)")
                        .HasColumnName("vat_rate");

                    b.HasKey("Id");

                    b.HasIndex("BookId");

                    b.HasIndex("OrderId");

                    b.ToTable("order_items", (string)null);
                });

            modelBuilder.Entity("LibraHub.Orders.Domain.Payments.Payment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("completed_at");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<DateTime?>("FailedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("failed_at");

                    b.Property<string>("FailureReason")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("failure_reason");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uuid")
                        .HasColumnName("order_id");

                    b.Property<int>("Provider")
                        .HasColumnType("integer")
                        .HasColumnName("provider");

                    b.Property<string>("ProviderReference")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("provider_reference");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.HasKey("Id");

                    b.HasIndex("OrderId")
                        .IsUnique();

                    b.HasIndex("ProviderReference");

                    b.HasIndex("Status");

                    b.ToTable("payments", (string)null);
                });

            modelBuilder.Entity("LibraHub.Orders.Domain.Refunds.Refund", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("OrderId")
                        .HasColumnType("uuid")
                        .HasColumnName("order_id");

                    b.Property<Guid>("PaymentId")
                        .HasColumnType("uuid")
                        .HasColumnName("payment_id");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("reason");

                    b.Property<DateTime>("RefundedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("refunded_at");

                    b.Property<Guid>("RefundedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("refunded_by");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("PaymentId");

                    b.ToTable("refunds", (string)null);
                });

            modelBuilder.Entity("LibraHub.Orders.Infrastructure.Idempotency.IdempotencyKey", b =>
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

            modelBuilder.Entity("LibraHub.Orders.Domain.Orders.Order", b =>
                {
                    b.OwnsOne("LibraHub.Orders.Domain.Orders.Money", "Subtotal", b1 =>
                        {
                            b1.Property<Guid>("OrderId")
                                .HasColumnType("uuid");

                            b1.Property<decimal>("Amount")
                                .HasPrecision(18, 2)
                                .HasColumnType("numeric(18,2)")
                                .HasColumnName("subtotal_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasMaxLength(3)
                                .HasColumnType("character varying(3)")
                                .HasColumnName("subtotal_currency");

                            b1.HasKey("OrderId");

                            b1.ToTable("orders");

                            b1.WithOwner()
                                .HasForeignKey("OrderId");
                        });

                    b.OwnsOne("LibraHub.Orders.Domain.Orders.Money", "Total", b1 =>
                        {
                            b1.Property<Guid>("OrderId")
                                .HasColumnType("uuid");

                            b1.Property<decimal>("Amount")
                                .HasPrecision(18, 2)
                                .HasColumnType("numeric(18,2)")
                                .HasColumnName("total_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasMaxLength(3)
                                .HasColumnType("character varying(3)")
                                .HasColumnName("total_currency");

                            b1.HasKey("OrderId");

                            b1.ToTable("orders");

                            b1.WithOwner()
                                .HasForeignKey("OrderId");
                        });

                    b.OwnsOne("LibraHub.Orders.Domain.Orders.Money", "VatTotal", b1 =>
                        {
                            b1.Property<Guid>("OrderId")
                                .HasColumnType("uuid");

                            b1.Property<decimal>("Amount")
                                .HasPrecision(18, 2)
                                .HasColumnType("numeric(18,2)")
                                .HasColumnName("vat_total_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasMaxLength(3)
                                .HasColumnType("character varying(3)")
                                .HasColumnName("vat_total_currency");

                            b1.HasKey("OrderId");

                            b1.ToTable("orders");

                            b1.WithOwner()
                                .HasForeignKey("OrderId");
                        });

                    b.Navigation("Subtotal")
                        .IsRequired();

                    b.Navigation("Total")
                        .IsRequired();

                    b.Navigation("VatTotal")
                        .IsRequired();
                });

            modelBuilder.Entity("LibraHub.Orders.Domain.Orders.OrderItem", b =>
                {
                    b.HasOne("LibraHub.Orders.Domain.Orders.Order", null)
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("LibraHub.Orders.Domain.Orders.Money", "BasePrice", b1 =>
                        {
                            b1.Property<Guid>("OrderItemId")
                                .HasColumnType("uuid");

                            b1.Property<decimal>("Amount")
                                .HasPrecision(18, 2)
                                .HasColumnType("numeric(18,2)")
                                .HasColumnName("base_price_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasMaxLength(3)
                                .HasColumnType("character varying(3)")
                                .HasColumnName("base_price_currency");

                            b1.HasKey("OrderItemId");

                            b1.ToTable("order_items");

                            b1.WithOwner()
                                .HasForeignKey("OrderItemId");
                        });

                    b.OwnsOne("LibraHub.Orders.Domain.Orders.Money", "FinalPrice", b1 =>
                        {
                            b1.Property<Guid>("OrderItemId")
                                .HasColumnType("uuid");

                            b1.Property<decimal>("Amount")
                                .HasPrecision(18, 2)
                                .HasColumnType("numeric(18,2)")
                                .HasColumnName("final_price_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasMaxLength(3)
                                .HasColumnType("character varying(3)")
                                .HasColumnName("final_price_currency");

                            b1.HasKey("OrderItemId");

                            b1.ToTable("order_items");

                            b1.WithOwner()
                                .HasForeignKey("OrderItemId");
                        });

                    b.OwnsOne("LibraHub.Orders.Domain.Orders.Money", "VatAmount", b1 =>
                        {
                            b1.Property<Guid>("OrderItemId")
                                .HasColumnType("uuid");

                            b1.Property<decimal>("Amount")
                                .HasPrecision(18, 2)
                                .HasColumnType("numeric(18,2)")
                                .HasColumnName("vat_amount_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasMaxLength(3)
                                .HasColumnType("character varying(3)")
                                .HasColumnName("vat_amount_currency");

                            b1.HasKey("OrderItemId");

                            b1.ToTable("order_items");

                            b1.WithOwner()
                                .HasForeignKey("OrderItemId");
                        });

                    b.Navigation("BasePrice")
                        .IsRequired();

                    b.Navigation("FinalPrice")
                        .IsRequired();

                    b.Navigation("VatAmount")
                        .IsRequired();
                });

            modelBuilder.Entity("LibraHub.Orders.Domain.Payments.Payment", b =>
                {
                    b.OwnsOne("LibraHub.Orders.Domain.Orders.Money", "Amount", b1 =>
                        {
                            b1.Property<Guid>("PaymentId")
                                .HasColumnType("uuid");

                            b1.Property<decimal>("Amount")
                                .HasPrecision(18, 2)
                                .HasColumnType("numeric(18,2)")
                                .HasColumnName("amount_amount");

                            b1.Property<string>("Currency")
                                .IsRequired()
                                .HasMaxLength(3)
                                .HasColumnType("character varying(3)")
                                .HasColumnName("amount_currency");

                            b1.HasKey("PaymentId");

                            b1.ToTable("payments");

                            b1.WithOwner()
                                .HasForeignKey("PaymentId");
                        });

                    b.Navigation("Amount")
                        .IsRequired();
                });

            modelBuilder.Entity("LibraHub.Orders.Domain.Orders.Order", b =>
                {
                    b.Navigation("Items");
                });
#pragma warning restore 612, 618
        }
    }
}
