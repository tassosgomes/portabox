using PortaBox.Modules.Gestao.Application.Audit;

namespace PortaBox.Modules.Gestao.UnitTests.Audit;

public class StructuralAuditMetadataTests
{
    [Fact]
    public void ForBlocoCriado_ShouldReturnExpectedSchema()
    {
        var blocoId = Guid.NewGuid();
        const string nome = "Torre Alfa";

        var metadata = StructuralAuditMetadata.ForBlocoCriado(blocoId, nome);

        Assert.Equal(["blocoId", "nome"], metadata.Keys);
        Assert.Equal(blocoId, metadata["blocoId"]);
        Assert.Equal(nome, metadata["nome"]);
    }

    [Fact]
    public void ForBlocoRenomeado_ShouldReturnExpectedSchema()
    {
        var blocoId = Guid.NewGuid();

        var metadata = StructuralAuditMetadata.ForBlocoRenomeado(blocoId, "Bloco A", "Torre Alfa");

        Assert.Equal(["blocoId", "nomeAntes", "nomeDepois"], metadata.Keys);
        Assert.Equal(blocoId, metadata["blocoId"]);
        Assert.Equal("Bloco A", metadata["nomeAntes"]);
        Assert.Equal("Torre Alfa", metadata["nomeDepois"]);
    }

    [Fact]
    public void ForBlocoInativado_ShouldReturnExpectedSchema()
    {
        var blocoId = Guid.NewGuid();

        var metadata = StructuralAuditMetadata.ForBlocoInativado(blocoId, "Torre Alfa");

        Assert.Equal(["blocoId", "nome"], metadata.Keys);
        Assert.Equal(blocoId, metadata["blocoId"]);
        Assert.Equal("Torre Alfa", metadata["nome"]);
    }

    [Fact]
    public void ForBlocoReativado_ShouldReturnExpectedSchema()
    {
        var blocoId = Guid.NewGuid();

        var metadata = StructuralAuditMetadata.ForBlocoReativado(blocoId, "Torre Alfa");

        Assert.Equal(["blocoId", "nome"], metadata.Keys);
        Assert.Equal(blocoId, metadata["blocoId"]);
        Assert.Equal("Torre Alfa", metadata["nome"]);
    }

    [Fact]
    public void ForUnidadeCriada_ShouldReturnExpectedSchema()
    {
        var unidadeId = Guid.NewGuid();
        var blocoId = Guid.NewGuid();

        var metadata = StructuralAuditMetadata.ForUnidadeCriada(unidadeId, blocoId, 2, "201A");

        Assert.Equal(["unidadeId", "blocoId", "andar", "numero"], metadata.Keys);
        Assert.Equal(unidadeId, metadata["unidadeId"]);
        Assert.Equal(blocoId, metadata["blocoId"]);
        Assert.IsType<int>(metadata["andar"]);
        Assert.Equal(2, metadata["andar"]);
        Assert.IsType<string>(metadata["numero"]);
        Assert.Equal("201A", metadata["numero"]);
    }

    [Fact]
    public void ForUnidadeInativada_ShouldReturnExpectedSchema()
    {
        var unidadeId = Guid.NewGuid();
        var blocoId = Guid.NewGuid();

        var metadata = StructuralAuditMetadata.ForUnidadeInativada(unidadeId, blocoId, 3, "302");

        Assert.Equal(["unidadeId", "blocoId", "andar", "numero"], metadata.Keys);
        Assert.Equal(unidadeId, metadata["unidadeId"]);
        Assert.Equal(blocoId, metadata["blocoId"]);
        Assert.Equal(3, metadata["andar"]);
        Assert.Equal("302", metadata["numero"]);
    }

    [Fact]
    public void ForUnidadeReativada_ShouldReturnExpectedSchema()
    {
        var unidadeId = Guid.NewGuid();
        var blocoId = Guid.NewGuid();

        var metadata = StructuralAuditMetadata.ForUnidadeReativada(unidadeId, blocoId, 4, "401B");

        Assert.Equal(["unidadeId", "blocoId", "andar", "numero"], metadata.Keys);
        Assert.Equal(unidadeId, metadata["unidadeId"]);
        Assert.Equal(blocoId, metadata["blocoId"]);
        Assert.Equal(4, metadata["andar"]);
        Assert.Equal("401B", metadata["numero"]);
    }
}
