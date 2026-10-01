/// <summary>
/// Contrato para estoques que permitem verificar se uma equipe ainda consegue
/// reconstruir a Base Soldado depois de perder sua economia.
/// Equipes novas podem implementar este contrato para entrar na verificacao.
/// </summary>
public interface IGameOverRecoveryEconomy
{
    string TagEquipe { get; }
    bool PodeReconstruirBaseSoldado { get; }
}

/// <summary>
/// Contrato para Bases Soldado que podem produzir coletores.
/// </summary>
public interface IGameOverRecoverySpawner
{
    bool TemRecursosParaCriarColetor();
}
