using DevPortfolio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPortfolio.Infrastructure.Persistence.Configurations;

// Espelha o script V1__CreateTable_Usuario.sql do DevPortfolio.Dados.
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.SenhaHash)
            .HasColumnName("senha_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Perfil)
            .HasColumnName("perfil")
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(u => u.Ativo)
            .HasColumnName("ativo")
            .IsRequired();

        builder.Property(u => u.DataCadastro)
            .HasColumnName("data_cadastro")
            .IsRequired();

        builder.Property(u => u.DataUltimoAcesso)
            .HasColumnName("data_ultimo_acesso");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ux_usuario_email");
    }
}
