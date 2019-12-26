﻿// <auto-generated />
using System;
using ISQExplorer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ISQExplorer.Migrations
{
    [DbContext(typeof(ISQExplorerContext))]
    partial class ISQExplorerContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("ISQExplorer.Models.CourseCodeModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("CourseCode")
                        .IsRequired()
                        .HasColumnType("character varying(12)")
                        .HasMaxLength(12);

                    b.Property<int>("CourseId")
                        .HasColumnType("integer");

                    b.Property<int?>("Season")
                        .HasColumnType("integer");

                    b.Property<int?>("Year")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.ToTable("CourseCodes");
                });

            modelBuilder.Entity("ISQExplorer.Models.CourseModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Description");

                    b.ToTable("Courses");
                });

            modelBuilder.Entity("ISQExplorer.Models.CourseNameModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("CourseId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("character varying(255)")
                        .HasMaxLength(255);

                    b.Property<int?>("Season")
                        .HasColumnType("integer");

                    b.Property<int?>("Year")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.ToTable("CourseNames");
                });

            modelBuilder.Entity("ISQExplorer.Models.ISQEntryModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int?>("CourseId")
                        .HasColumnType("integer");

                    b.Property<int>("Crn")
                        .HasColumnType("integer");

                    b.Property<double>("MeanGpa")
                        .HasColumnType("double precision");

                    b.Property<int>("NEnrolled")
                        .HasColumnType("integer");

                    b.Property<int>("NResponded")
                        .HasColumnType("integer");

                    b.Property<int>("NTotal")
                        .HasColumnType("integer");

                    b.Property<double>("Pct1")
                        .HasColumnType("double precision");

                    b.Property<double>("Pct2")
                        .HasColumnType("double precision");

                    b.Property<double>("Pct3")
                        .HasColumnType("double precision");

                    b.Property<double>("Pct4")
                        .HasColumnType("double precision");

                    b.Property<double>("Pct5")
                        .HasColumnType("double precision");

                    b.Property<double>("PctA")
                        .HasColumnType("double precision");

                    b.Property<double>("PctAMinus")
                        .HasColumnType("double precision");

                    b.Property<double>("PctB")
                        .HasColumnType("double precision");

                    b.Property<double>("PctBMinus")
                        .HasColumnType("double precision");

                    b.Property<double>("PctBPlus")
                        .HasColumnType("double precision");

                    b.Property<double>("PctC")
                        .HasColumnType("double precision");

                    b.Property<double>("PctCPlus")
                        .HasColumnType("double precision");

                    b.Property<double>("PctD")
                        .HasColumnType("double precision");

                    b.Property<double>("PctF")
                        .HasColumnType("double precision");

                    b.Property<double>("PctNa")
                        .HasColumnType("double precision");

                    b.Property<double>("PctWithdraw")
                        .HasColumnType("double precision");

                    b.Property<int?>("ProfessorId")
                        .HasColumnType("integer");

                    b.Property<int>("Season")
                        .HasColumnType("integer");

                    b.Property<int>("Year")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.HasIndex("ProfessorId");

                    b.HasIndex("Crn", "Season", "Year");

                    b.ToTable("IsqEntries");
                });

            modelBuilder.Entity("ISQExplorer.Models.ProfessorModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("FirstName")
                        .HasColumnType("text");

                    b.Property<string>("LastName")
                        .HasColumnType("text");

                    b.Property<string>("NNumber")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("NNumber");

                    b.ToTable("Professors");
                });

            modelBuilder.Entity("ISQExplorer.Models.QueryModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("CourseCode")
                        .HasColumnType("text");

                    b.Property<string>("CourseName")
                        .HasColumnType("text");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ProfessorName")
                        .HasColumnType("text");

                    b.Property<int?>("SeasonSince")
                        .HasColumnType("integer");

                    b.Property<int?>("SeasonUntil")
                        .HasColumnType("integer");

                    b.Property<int?>("YearSince")
                        .HasColumnType("integer");

                    b.Property<int?>("YearUntil")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Queries");
                });

            modelBuilder.Entity("ISQExplorer.Models.CourseCodeModel", b =>
                {
                    b.HasOne("ISQExplorer.Models.CourseModel", "Course")
                        .WithMany()
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ISQExplorer.Models.CourseNameModel", b =>
                {
                    b.HasOne("ISQExplorer.Models.CourseModel", "Course")
                        .WithMany()
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ISQExplorer.Models.ISQEntryModel", b =>
                {
                    b.HasOne("ISQExplorer.Models.CourseModel", "Course")
                        .WithMany()
                        .HasForeignKey("CourseId");

                    b.HasOne("ISQExplorer.Models.ProfessorModel", "Professor")
                        .WithMany()
                        .HasForeignKey("ProfessorId");
                });
#pragma warning restore 612, 618
        }
    }
}
