using UnityEngine;

/// <summary>
/// ORQUESTRADOR CENTRAL DO AVIÃO.
///
/// Controla a sequência de fases em ordem de prioridade.
/// Apenas UMA fase roda por vez — todas as outras ficam desativadas.
///
/// COMO ADICIONAR UMA NOVA FASE NO FUTURO:
///   1. Adicione o valor no enum Fase
///   2. Adicione o componente no Inspector e em DesativarTudo()
///   3. Em IniciarFase(), adicione o case com o que ativar
///   4. Em ChecarTransicao(), adicione a condição de conclusão
/// </summary>
[DisallowMultipleComponent]
public class AviaoControler : MonoBehaviour
{
    // =====================================================================
    // FASES — em ordem de execução
    // =====================================================================

    public enum Fase
    {
        Garagem,    // 1°: move até o ponto de decolagem e rotaciona
        Voo,        // 2°: rolagem na pista, decolagem e subida
        // Patrulha // 3°: (futuro)
    }

    // =====================================================================
    // INSPECTOR
    // =====================================================================

    [Header("Componentes")]
    [SerializeField] private AviaoGaragem aviaoGaragem;
    [SerializeField] private AviaoVoo     aviaoVoo;

    // Adicione os próximos aqui:
    // [SerializeField] private AviaoVisao  aviaoVisao;

    [Header("Voo")]
    [Tooltip("Altura acima do terrain para considerar decolagem concluída")]
    [SerializeField] private float alturaSegura = 25f;

    [Header("Estado — somente leitura")]
    [SerializeField] private Fase  faseAtual;
    [SerializeField] private float alturaAcimaTerrain;

    // =====================================================================
    // PRIVADOS
    // =====================================================================

    private Terrain terrainRef;

    // =====================================================================
    // PROPRIEDADES PÚBLICAS
    // =====================================================================

    public Fase    FaseAtual      => faseAtual;
    public AviaoVoo Voo           => aviaoVoo;
    public bool    Operacional    => faseAtual == Fase.Voo; // expanda conforme as fases crescerem

    // =====================================================================
    // AWAKE — desativa tudo antes de qualquer outro script rodar
    // =====================================================================

    private void Awake()
    {
        terrainRef = Terrain.activeTerrain;
        DesativarTudo();
    }

    // =====================================================================
    // START — inicia a primeira fase
    // =====================================================================

    private void Start()
    {
        IniciarFase(Fase.Garagem);
    }

    // =====================================================================
    // UPDATE
    // =====================================================================

    private void Update()
    {
        AtualizarAltura();
        ChecarTransicao();
    }

    // =====================================================================
    // INICIAR FASE
    // =====================================================================

    private void IniciarFase(Fase fase)
    {
        faseAtual = fase;
        DesativarTudo();

        switch (fase)
        {
            case Fase.Garagem:
                if (aviaoGaragem != null) aviaoGaragem.enabled = true;
                break;

            case Fase.Voo:
                if (aviaoVoo != null) aviaoVoo.enabled = true;
                break;

            // Adicione os próximos cases aqui:
            // case Fase.Patrulha:
            //     if (aviaoVisao != null) aviaoVisao.enabled = true;
            //     break;
        }
    }

    // =====================================================================
    // CHECAR TRANSIÇÃO
    // =====================================================================

    private void ChecarTransicao()
    {
        switch (faseAtual)
        {
            case Fase.Garagem:
                // AviaoGaragem se desabilita sozinho ao terminar
                if (aviaoGaragem != null && !aviaoGaragem.enabled)
                    IniciarFase(Fase.Voo);
                break;

            case Fase.Voo:
                // Aguarda o avião estar no ar e acima da altitude segura
                if (aviaoVoo != null
                    && aviaoVoo.EstadoAtual == AviaoVoo.EstadoVoo.EmVoo
                    && alturaAcimaTerrain >= alturaSegura)
                {
                    // IniciarFase(Fase.Patrulha); // próxima fase quando existir
                }
                break;

            // Adicione os próximos cases aqui
        }
    }

    // =====================================================================
    // DESATIVAR TUDO
    // =====================================================================

    private void DesativarTudo()
    {
        if (aviaoGaragem != null) aviaoGaragem.enabled = false;
        if (aviaoVoo     != null) aviaoVoo.enabled     = false;

        // Adicione os próximos componentes aqui:
        // if (aviaoVisao != null) aviaoVisao.enabled = false;
    }

    // =====================================================================
    // ALTITUDE
    // =====================================================================

    private void AtualizarAltura()
    {
        if (aviaoVoo != null && aviaoVoo.enabled)
        {
            alturaAcimaTerrain = aviaoVoo.AlturaAcimaTerrain;
            return;
        }

        // Fallback por raycast quando AviaoVoo não está ativo
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 800f))
            alturaAcimaTerrain = transform.position.y - hit.point.y;
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || terrainRef == null) return;

        float alturaTerrain = terrainRef.SampleHeight(transform.position)
                            + terrainRef.transform.position.y;
        float yLinha = alturaTerrain + alturaSegura;
        Vector3 p    = transform.position;

        Gizmos.color = faseAtual == Fase.Voo ? Color.green : Color.yellow;
        Gizmos.DrawLine(new Vector3(p.x - 15f, yLinha, p.z),
                        new Vector3(p.x + 15f, yLinha, p.z));
    }
}
