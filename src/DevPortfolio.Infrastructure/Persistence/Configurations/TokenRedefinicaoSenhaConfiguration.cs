using DevPortfolio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPortfolio.Infrastructure.Persistence.Configurations;

// Espelha o script V2__CreateTable_TokenRedefinicaoSenha.sql do DevPortfolio.Dados.
public class TokenRedefinicaoSenhaConfiguration : IEntityTypeConfiguration<TokenRedefinicaoSenha>
{
    public void Configure(EntityTypeBuilder<TokenRedefinicaoSenha> builder)
    {
        builder.ToTable("token_redefinicao_senha");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();

        builder.Property(t => t.DataExpiracao)
            .HasColumnName("data_expiracao")
            .IsRequired();

        builder.Property(t => t.DataUtilizacao)
            .HasColumnName("data_utilizacao");

        builder.Property(t => t.DataCadastro)
            .HasColumnName("data_cadastro")
            .IsRequired();

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_token_redefinicao_senha_token_hash");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .HasConstraintName("fk_token_redefinicao_senha_usuario")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
