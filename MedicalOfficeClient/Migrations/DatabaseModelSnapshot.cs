using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using MedicalOfficeClient.Services;

namespace MedicalOfficeClient.Migrations
{
    [DbContext(typeof(Database))]
    partial class DatabaseModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.1");

            modelBuilder.Entity("MedicalOfficeClient.Services.MedicalCase", b =>
                {
                    b.Property<Guid>("MedicalCaseId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("DateChanged");

                    b.Property<DateTime?>("DateCreated");

                    b.Property<string>("Label");

                    b.Property<Guid>("PersonId");

                    b.Property<int>("Type");

                    b.Property<string>("UserChanged");

                    b.Property<string>("UserCreated");

                    b.HasKey("MedicalCaseId");

                    b.HasIndex("PersonId");

                    b.ToTable("MedicalCases");
                });

            modelBuilder.Entity("MedicalOfficeClient.Services.MedicalItem", b =>
                {
                    b.Property<Guid>("MedicalItemId")
                        .ValueGeneratedOnAdd();

                    b.Property<byte[]>("Content");

                    b.Property<DateTime?>("DateChanged");

                    b.Property<DateTime?>("DateCreated");

                    b.Property<byte[]>("Element");

                    b.Property<string>("Label");

                    b.Property<Guid>("MedicalCaseId");

                    b.Property<byte[]>("Overlay");

                    b.Property<byte[]>("Preview");

                    b.Property<int>("Type");

                    b.Property<string>("UserChanged");

                    b.Property<string>("UserCreated");

                    b.HasKey("MedicalItemId");

                    b.HasIndex("MedicalCaseId");

                    b.ToTable("MedicalItems");
                });

            modelBuilder.Entity("MedicalOfficeClient.Services.Person", b =>
                {
                    b.Property<Guid>("PersonId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Birthday");

                    b.Property<DateTime?>("DateChanged");

                    b.Property<DateTime?>("DateCreated");

                    b.Property<string>("FirstName")
                        .IsRequired();

                    b.Property<string>("LastName")
                        .IsRequired();

                    b.Property<string>("Title");

                    b.Property<string>("UserChanged");

                    b.Property<string>("UserCreated");

                    b.HasKey("PersonId");

                    b.ToTable("Persons");
                });

            modelBuilder.Entity("MedicalOfficeClient.Services.MedicalCase", b =>
                {
                    b.HasOne("MedicalOfficeClient.Services.Person", "Person")
                        .WithMany("Cases")
                        .HasForeignKey("PersonId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MedicalOfficeClient.Services.MedicalItem", b =>
                {
                    b.HasOne("MedicalOfficeClient.Services.MedicalCase", "MedicalCase")
                        .WithMany("Items")
                        .HasForeignKey("MedicalCaseId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
