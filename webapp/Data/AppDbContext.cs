using System;
using System.Collections.Generic;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Assistante> Assistantes { get; set; }

    public virtual DbSet<Ca> Cas { get; set; }

    public virtual DbSet<FiltreHistorique> FiltreHistoriques { get; set; }

    public virtual DbSet<FiltreOperatrice> FiltreOperatrices { get; set; }

    public virtual DbSet<FranceOptique> FranceOptiques { get; set; }

    public virtual DbSet<Historique> Historiques { get; set; }

    public virtual DbSet<LoginHistory> LoginHistories { get; set; }

    public virtual DbSet<Pays> Pays { get; set; }

    public virtual DbSet<TypeFamille> TypeFamilles { get; set; }

    public virtual DbSet<TypeFranchise> TypeFranchises { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assistante>(entity =>
        {
            entity.HasKey(e => e.PrenomNom).HasName("assistantes_pkey");

            entity.Property(e => e.AllFiltres)
                .HasDefaultValue(false)
                .HasComment("Deprecated 2026-08-14: superseded by users.role (admin/assistant see all, commercial sees own only). Kept until the app is fully switched over to reading users.role.");
        });

        modelBuilder.Entity<Ca>(entity =>
        {
            entity.HasKey(e => new { e.CleOpl, e.Annee }).HasName("ca_pkey");

            entity.Property(e => e.Annee).HasDefaultValue(0);
            entity.Property(e => e.Ca1).HasDefaultValue(0);
            entity.Property(e => e.IdCa)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn();

            entity.HasOne(d => d.CleOplNavigation).WithMany(p => p.Cas)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ca_cle_opl");
        });

        modelBuilder.Entity<FiltreHistorique>(entity =>
        {
            entity.HasKey(e => new { e.NomOperateur, e.NomFiltre }).HasName("filtre_historique_pkey");
        });

        modelBuilder.Entity<FiltreOperatrice>(entity =>
        {
            entity.HasKey(e => new { e.NomOperateur, e.NomFiltre }).HasName("filtre_operatrice_pkey");
        });

        modelBuilder.Entity<FranceOptique>(entity =>
        {
            entity.HasKey(e => e.CleOpl).HasName("france_optique_pkey");

            entity.Property(e => e.CleOpl).ValueGeneratedNever();
            entity.Property(e => e.Bloque).HasDefaultValue(false);
            entity.Property(e => e.EtatClient).HasDefaultValueSql("'0'::character varying");
            entity.Property(e => e.FacturationElectronique).HasDefaultValue(false);
            entity.Property(e => e.PasEmailing).HasDefaultValue(false);
            entity.Property(e => e.RappelRdv).HasDefaultValue(false);
            entity.Property(e => e.StatutsClients).HasDefaultValueSql("'-1'::character varying");
            entity.Property(e => e.ValidationStatus).HasDefaultValueSql("'pending'::character varying");

            // EF appends this to EVERY FranceOptiques query, so refused clients are
            // hidden app-wide. Opt out per query with IgnoreQueryFilters().
            entity.HasQueryFilter(c => c.ValidationStatus != "refused");

            entity.HasOne(d => d.FamilleNavigation).WithMany(p => p.FranceOptiques)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_france_optique_famille");

            entity.HasOne(d => d.FranchiseNavigation).WithMany(p => p.FranceOptiqueFranchiseNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_france_optique_franchise");

            entity.HasOne(d => d.Franchise2Navigation).WithMany(p => p.FranceOptiqueFranchise2Navigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_france_optique_franchise2");

            entity.HasOne(d => d.Franchise3Navigation).WithMany(p => p.FranceOptiqueFranchise3Navigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_france_optique_franchise3");

            entity.HasOne(d => d.Franchise4Navigation).WithMany(p => p.FranceOptiqueFranchise4Navigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_france_optique_franchise4");

            entity.HasOne(d => d.PaysNavigation).WithMany(p => p.FranceOptiques)
                .HasPrincipalKey(p => p.NomPays)
                .HasForeignKey(d => d.Pays)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_france_optique_pays");
        });

        modelBuilder.Entity<Historique>(entity =>
        {
            entity.HasKey(e => e.IdHisto).HasName("historique_pkey");

            entity.Property(e => e.IdHisto).UseIdentityAlwaysColumn();
            entity.Property(e => e.StatutsClients).HasDefaultValueSql("'-1'::character varying");

            entity.HasOne(d => d.NumClientNavigation).WithMany(p => p.Historiques)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_historique_num_client");
        });

        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("login_history_pkey");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.LoggedInAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.LoginNavigation).WithMany(p => p.LoginHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("login_history_login_fkey");
        });

        modelBuilder.Entity<Pays>(entity =>
        {
            entity.HasKey(e => e.IdPays).HasName("pays_pkey");

            entity.Property(e => e.IdPays).UseIdentityAlwaysColumn();
        });

        modelBuilder.Entity<TypeFamille>(entity =>
        {
            entity.HasKey(e => e.NomFamille).HasName("type_famille_pkey");
        });

        modelBuilder.Entity<TypeFranchise>(entity =>
        {
            entity.HasKey(e => e.NomFranchise).HasName("type_franchise_pkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Login).HasName("users_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.FailedLoginCount).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MustChangePassword).HasDefaultValue(true);
        });
        modelBuilder.HasSequence("bon_commande_number_seq");
        modelBuilder.HasSequence("france_optique_cle_opl_seq");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
