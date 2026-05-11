using UnityEngine;

/// <summary>
/// ORQUESTRADOR CENTRAL DO AVIÃO.
///
/// Controla a sequência de fases em ordem de prioridade estrita.
/// Apenas os componentes da fase atual ficam habilitados.
///
/// Fases em ordem:
///   1. Garagem  — move até o ponto de decolagem e rotaciona para a pista
///   2. Voo      — rolagem, decolagem e subida até alturaTransicaoPatrulha
///   3. Patrulha — AviaoVisao assume o movimento + AviaoAtaque fica ativo
///
/// AviaoAtaque é ativado JUNTO com AviaoVisao na fase Patrulha.
/// Ele fica passivo enquanto AviaoVisao não reportar um alvo.
///
/// SETUP (Script Execution Order recomendado):
///   AviaoControler = -200
///   AviaoVoo       = -100
/// </summary>
[DisallowMultipleComponent]
public class AviaoControler : MonoBehaviour
{
    // =====================================================================
    // FASES
    // =====================================================================

    public enum Fase
    {
        Garagem,    // 1°: posiciona o avião na cabeceira da pista
        Voo,        // 2°: rolagem + decolagem + subida
        Patrulha,   // 3°: AviaoVisao + AviaoAtaque ativos
    }

    // =====================================================================
    // INSPECTOR
    // =====================================================================

    [Header("Componentes")]
    [SerializeField] private AviaoGaragem aviaoGaragem;
    [SerializeField] private AviaoVoo     aviaoVoo;
    [SerializeField] private AviaoVisao   aviaoVisao;
    [SerializeField] private AviaoAtaque  aviaoAtaque;

    [Header("Transição Voo → Patrulha")]
    [Tooltip("Altura acima do terrain (metros) para desligar AviaoVoo e ligar AviaoVisao + AviaoAtaque")]
    [SerializeField] private float alturaTransicaoPatrulha = 100f;

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

    public Fase       FaseAtual          => faseAtual;
    public float      AlturaAcimaTerrain => alturaAcimaTerrain;
    public AviaoVisao Visao              => aviaoVisao;
    public AviaoAtaque Ataque            => aviaoAtaque;
    public bool       EmPatrulha         => faseAtual == Fase.Patrulha;

    // =====================================================================
    // AWAKE — desativa tudo antes de qualquer outro script rodar
    // =====================================================================

    private void Awake()
    {
        // Auto-referências
        if (aviaoGaragem == null) aviaoGaragem = GetComponent<AviaoGaragem>();
        if (aviaoVoo     == null) aviaoVoo     = GetComponent<AviaoVoo>();
        if (aviaoVisao   == null) aviaoVisao   = GetComponent<AviaoVisao>();
        if (aviaoAtaque  == null) aviaoAtaque  = GetComponent<AviaoAtaque>();

        terrainRef = Terrain.activeTerrain;

        // CRÍTICO: desativa TUDO antes de qualquer Update rodar.
        // Isso garante que Garagem, Voo, Visao e Ataque não executam
        // código ao mesmo tempo e causam o comportamento errático.
        DesativarTudo();
    }

    // =====================================================================
    // START — inicia a primeira fase após o Awake de todos os scripts
    // =====================================================================

    private void Start()
    {
        IniciarFase(Fase.Garagem);
    }

    // =====================================================================
    // UPDATE — monitora transições entre fases
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
            // ── GARAGEM ───────────────────────────────────────────────────
            case Fase.Garagem:
                if (aviaoGaragem != null)
                    aviaoGaragem.enabled = true;
                break;

            // ── VOO ───────────────────────────────────────────────────────
            case Fase.Voo:
                if (aviaoVoo != null)
                    aviaoVoo.enabled = true;
                break;

            // ── PATRULHA ─────────────────────────────────────────────────
            // AviaoVoo fica DESLIGADO — AviaoVisao assume o movimento.
            // AviaoAtaque é ativado JUNTO: fica em standby até AviaoVisao
            // detectar um inimigo e AviaoAtaque confirmar mira alinhada.
            case Fase.Patrulha:
                if (aviaoVisao  != null) aviaoVisao.enabled  = true;
                if (aviaoAtaque != null) aviaoAtaque.enabled = true;
                break;
        }

        Debug.Log($"[AviaoControler] {gameObject.name} → Fase: {fase}", this);
    }

    // =====================================================================
    // CHECAR TRANSIÇÃO
    // =====================================================================

    private void ChecarTransicao()
    {
        switch (faseAtual)
        {
            // Garagem se desabilita sozinho quando conclui → inicia Voo
            case Fase.Garagem:
                if (aviaoGaragem == null || !aviaoGaragem.enabled)
                    IniciarFase(Fase.Voo);
                break;

            // Avião atingiu altitude de transição em estado EmVoo → inicia Patrulha
            case Fase.Voo:
                if (aviaoVoo != null
                    && aviaoVoo.EstadoAtual == AviaoVoo.EstadoVoo.EmVoo
                    && alturaAcimaTerrain  >= alturaTransicaoPatrulha)
                {
                    IniciarFase(Fase.Patrulha);
                }
                break;

            // Na Patrulha o AviaoVisao e o AviaoAtaque gerenciam tudo.
            // Sem transição automática a partir daqui.
            case Fase.Patrulha:
                break;
        }
    }

    // =====================================================================
    // DESATIVAR TUDO
    // =====================================================================

    private void DesativarTudo()
    {
        if (aviaoGaragem != null) aviaoGaragem.enabled = false;
        if (aviaoVoo     != null) aviaoVoo.enabled     = false;
        if (aviaoVisao   != null) aviaoVisao.enabled   = false;
        if (aviaoAtaque  != null) aviaoAtaque.enabled  = false;
    }

    // =====================================================================
    // ALTITUDE
    // =====================================================================

    private void AtualizarAltura()
    {
        // Durante o Voo usa AviaoVoo diretamente (mais preciso)
        if (faseAtual == Fase.Voo && aviaoVoo != null && aviaoVoo.enabled)
        {
            alturaAcimaTerrain = aviaoVoo.AlturaAcimaTerrain;
            return;
        }

        // Fallback: raycast com tag "Terrain"
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 800f))
        {
            if (hit.collider.CompareTag("Terrain"))
                alturaAcimaTerrain = transform.position.y - hit.point.y;
        }
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (terrainRef == null) return;

        float   hTerrain = terrainRef.SampleHeight(transform.position)
                         + terrainRef.transform.position.y;
        float   yLinha   = hTerrain + alturaTransicaoPatrulha;
        Vector3 p        = transform.position;

        // Linha horizontal indicando a altitude de transição
        Gizmos.color = faseAtual == Fase.Patrulha ? Color.green : Color.yellow;
        Gizmos.DrawLine(new Vector3(p.x - 20f, yLinha, p.z),
                        new Vector3(p.x + 20f, yLinha, p.z));
    }
}