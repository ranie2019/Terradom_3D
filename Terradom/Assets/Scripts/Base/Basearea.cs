using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class BaseArea : MonoBehaviour
{
    private enum TipoBaseAtual
    {
        Nenhuma,
        Soldado,
        Tank,
        Aviao,
        TorreTerra
    }

    [Header("Prefab Base Soldado")]
    [SerializeField] private GameObject prefabBaseSoldado;

    [Header("Prefab Base Tank / Veiculo")]
    [SerializeField] private GameObject prefabBaseTank;

    [Header("Prefab Base Aviao")]
    [SerializeField] private GameObject prefabBaseAviao;

    [Header("Prefab Torre Terra")]
    [SerializeField] private GameObject prefabTorreTerra;

    [Header("Referencias")]
    [SerializeField] private Camera cameraPrincipal;
    [SerializeField] private LayerMask camadaDoChao = ~0;
    [SerializeField] private LayerMask camadaBloqueio = ~0;

    [Header("Rotacao com botao direito")]
    [SerializeField] private float velocidadeRotacaoMouse = 0.35f;

    [Header("Colisao")]
    [SerializeField] private float margemColisao = 0.95f;
    [SerializeField] private bool bloquearQualquerObjeto = true;

    [Header("Estado Atual")]
    [SerializeField] private TipoBaseAtual tipoBaseAtual = TipoBaseAtual.Nenhuma;
    [SerializeField] private GameObject baseSoldadoAtual;
    [SerializeField] private GameObject baseTankAtual;
    [SerializeField] private GameObject baseAviaoAtual;
    [SerializeField] private GameObject torreTerraAtual;
    [SerializeField] private bool estaPosicionando;
    [SerializeField] private bool podeConstruir;
    [SerializeField] private bool modoRotacao;

    private void Awake()
    {
        if (cameraPrincipal == null)
            cameraPrincipal = Camera.main;
    }

    private void Update()
    {
        GameObject baseAtual = ObterBaseAtual();

        if (!estaPosicionando || baseAtual == null)
            return;

        AlternarModoComBotaoDireito();

        if (modoRotacao)
            RotacionarComMouse(baseAtual);
        else
            AtualizarPosicaoComMouse(baseAtual);

        VerificarColisao(baseAtual);
        ConfirmarComBotaoEsquerdo();
    }

    // =====================================================================
    // PUBLICOS — chamados pelos botoes da UI
    // =====================================================================

    public void CriarBaseParaPosicionar()
    {
        CriarBaseSoldadoParaPosicionar();
    }

    public void CriarBaseSoldadoParaPosicionar()
    {
        if (GameControllerRecursos.Instance == null) return;

        IniciarPosicionamentoDaBase(
            TipoBaseAtual.Soldado,
            prefabBaseSoldado,
            GameControllerRecursos.Instance.GetCustoPedraBaseSoldado(),
            GameControllerRecursos.Instance.GetCustoMadeiraBaseSoldado(),
            GameControllerRecursos.Instance.GetCustoMetalBaseSoldado()
        );
    }

    public void CriarBaseTankParaPosicionar()
    {
        if (GameControllerRecursos.Instance == null) return;

        IniciarPosicionamentoDaBase(
            TipoBaseAtual.Tank,
            prefabBaseTank,
            GameControllerRecursos.Instance.GetCustoPedraBaseVeiculo(),
            GameControllerRecursos.Instance.GetCustoMadeiraBaseVeiculo(),
            GameControllerRecursos.Instance.GetCustoMetalBaseVeiculo()
        );
    }

    public void CriarBaseVeiculoParaPosicionar()
    {
        CriarBaseTankParaPosicionar();
    }

    public void CriarBaseAviaoParaPosicionar()
    {
        if (GameControllerRecursos.Instance == null) return;

        IniciarPosicionamentoDaBase(
            TipoBaseAtual.Aviao,
            prefabBaseAviao,
            GameControllerRecursos.Instance.GetCustoPedraBaseAviao(),
            GameControllerRecursos.Instance.GetCustoMadeiraBaseAviao(),
            GameControllerRecursos.Instance.GetCustoMetalBaseAviao()
        );
    }

    public void CriarTorreTerraParaPosicionar()
    {
        if (GameControllerRecursos.Instance == null) return;

        IniciarPosicionamentoDaBase(
            TipoBaseAtual.TorreTerra,
            prefabTorreTerra,
            GameControllerRecursos.Instance.GetCustoPedraTorreTerra(),
            GameControllerRecursos.Instance.GetCustoMadeiraTorreTerra(),
            GameControllerRecursos.Instance.GetCustoMetalTorreTerra()
        );
    }

    // Mantidos para compatibilidade com código externo que chama esses métodos
    public bool PodeCriarBaseSoldado()
    {
        return GameControllerRecursos.Instance != null &&
               GameControllerRecursos.Instance.PodeCriarBaseSoldado();
    }

    public bool PodeCriarBaseTank()
    {
        return GameControllerRecursos.Instance != null &&
               GameControllerRecursos.Instance.PodeCriarBaseVeiculo();
    }

    public bool PodeCriarBaseVeiculo()
    {
        return PodeCriarBaseTank();
    }

    public bool PodeCriarBaseAviao()
    {
        return GameControllerRecursos.Instance != null &&
               GameControllerRecursos.Instance.PodeCriarBaseAviao();
    }

    public bool PodeCriarTorreTerra()
    {
        return GameControllerRecursos.Instance != null &&
               GameControllerRecursos.Instance.PodeCriarTorreTerra();
    }

    // =====================================================================
    // PRIVADOS
    // =====================================================================

    private void IniciarPosicionamentoDaBase(
        TipoBaseAtual tipoBase,
        GameObject prefabEscolhido,
        int custoPedra,
        int custoMadeira,
        int custoMetal
    )
    {
        if (prefabEscolhido == null)
            return;

        if (estaPosicionando)
            return;

        if (!GastarRecursos(custoPedra, custoMadeira, custoMetal))
            return;

        LimparBaseAtualEmPosicionamento();

        GameObject novaBase = Instantiate(prefabEscolhido);
        novaBase.name = prefabEscolhido.name;
        novaBase.SetActive(true);

        tipoBaseAtual = tipoBase;
        DefinirBaseAtual(tipoBase, novaBase);

        estaPosicionando = true;
        podeConstruir = false;
        modoRotacao = false;

        DesativarScriptsDaBase(novaBase);
        PosicionarBaseNoCentroDaTela(novaBase);
        VerificarColisao(novaBase);
    }

    private bool GastarRecursos(int custoPedra, int custoMadeira, int custoMetal)
    {
        if (GameControllerRecursos.Instance == null)
            return false;

        return GameControllerRecursos.Instance.TentarGastarRecursos(custoPedra, custoMadeira, custoMetal);
    }

    private GameObject ObterBaseAtual()
    {
        switch (tipoBaseAtual)
        {
            case TipoBaseAtual.Soldado:    return baseSoldadoAtual;
            case TipoBaseAtual.Tank:       return baseTankAtual;
            case TipoBaseAtual.Aviao:      return baseAviaoAtual;
            case TipoBaseAtual.TorreTerra: return torreTerraAtual;
            default:                       return null;
        }
    }

    private void DefinirBaseAtual(TipoBaseAtual tipoBase, GameObject novaBase)
    {
        baseSoldadoAtual = null;
        baseTankAtual    = null;
        baseAviaoAtual   = null;
        torreTerraAtual  = null;

        switch (tipoBase)
        {
            case TipoBaseAtual.Soldado:    baseSoldadoAtual = novaBase; break;
            case TipoBaseAtual.Tank:       baseTankAtual    = novaBase; break;
            case TipoBaseAtual.Aviao:      baseAviaoAtual   = novaBase; break;
            case TipoBaseAtual.TorreTerra: torreTerraAtual  = novaBase; break;
        }
    }

    private void LimparBaseAtualEmPosicionamento()
    {
        GameObject baseAtual = ObterBaseAtual();

        if (baseAtual != null)
            Destroy(baseAtual);

        baseSoldadoAtual = null;
        baseTankAtual    = null;
        baseAviaoAtual   = null;
        torreTerraAtual  = null;
        tipoBaseAtual    = TipoBaseAtual.Nenhuma;
    }

    private void AlternarModoComBotaoDireito()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        modoRotacao = !modoRotacao;
    }

    private void AtualizarPosicaoComMouse(GameObject baseAtual)
    {
        if (Mouse.current == null || cameraPrincipal == null || baseAtual == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (TentarPegarPontoNoChao(mousePos, baseAtual, out Vector3 ponto))
        {
            ponto.y = 0f;
            baseAtual.transform.position = ponto;
        }
    }

    private void RotacionarComMouse(GameObject baseAtual)
    {
        if (Mouse.current == null || baseAtual == null)
            return;

        Vector2 deltaMouse = Mouse.current.delta.ReadValue();
        float rotacaoY = deltaMouse.y * velocidadeRotacaoMouse;

        baseAtual.transform.Rotate(Vector3.up, rotacaoY, Space.World);
    }

    private void PosicionarBaseNoCentroDaTela(GameObject baseAtual)
    {
        if (cameraPrincipal == null || baseAtual == null)
            return;

        Vector2 centroTela = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (TentarPegarPontoNoChao(centroTela, baseAtual, out Vector3 ponto))
        {
            ponto.y = 0f;
            baseAtual.transform.position = ponto;
        }
    }

    private bool TentarPegarPontoNoChao(Vector2 posicaoTela, GameObject baseAtual, out Vector3 ponto)
    {
        ponto = Vector3.zero;

        if (cameraPrincipal == null)
            return false;

        Ray ray = cameraPrincipal.ScreenPointToRay(posicaoTela);

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, camadaDoChao, QueryTriggerInteraction.Ignore))
        {
            if (baseAtual != null && hit.collider.transform.IsChildOf(baseAtual.transform))
                return UsarPlanoYZero(ray, out ponto);

            ponto = hit.point;
            return true;
        }

        return UsarPlanoYZero(ray, out ponto);
    }

    private bool UsarPlanoYZero(Ray ray, out Vector3 ponto)
    {
        ponto = Vector3.zero;

        Plane plano = new Plane(Vector3.up, Vector3.zero);

        if (plano.Raycast(ray, out float distancia))
        {
            ponto = ray.GetPoint(distancia);
            return true;
        }

        return false;
    }

    private void ConfirmarComBotaoEsquerdo()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!podeConstruir)
            return;

        TravarBase();
    }

    private void TravarBase()
    {
        GameObject baseAtual = ObterBaseAtual();

        if (baseAtual == null)
            return;

        estaPosicionando = false;
        podeConstruir    = true;
        modoRotacao      = false;

        AtivarScriptsDaBase(baseAtual);

        tipoBaseAtual    = TipoBaseAtual.Nenhuma;
        baseSoldadoAtual = null;
        baseTankAtual    = null;
        baseAviaoAtual   = null;
        torreTerraAtual  = null;
    }

    private void VerificarColisao(GameObject baseAtual)
    {
        if (baseAtual == null)
        {
            podeConstruir = false;
            return;
        }

        if (!TentarCalcularBoundsDaBase(baseAtual, out Bounds boundsBase))
        {
            podeConstruir = true;
            return;
        }

        Collider[] colisores = Physics.OverlapBox(
            boundsBase.center,
            boundsBase.extents * margemColisao,
            baseAtual.transform.rotation,
            camadaBloqueio,
            QueryTriggerInteraction.Ignore
        );

        podeConstruir = true;

        for (int i = 0; i < colisores.Length; i++)
        {
            Collider col = colisores[i];

            if (col == null) continue;
            if (col.transform.IsChildOf(baseAtual.transform)) continue;
            if (col.gameObject == baseAtual) continue;
            if (EhChao(col)) continue;
            if (!bloquearQualquerObjeto && !EhBase(col.transform)) continue;

            podeConstruir = false;
            return;
        }
    }

    private bool TentarCalcularBoundsDaBase(GameObject obj, out Bounds boundsFinal)
    {
        boundsFinal = new Bounds();

        if (obj == null)
            return false;

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        bool encontrouCollider = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];

            if (col == null) continue;

            if (!encontrouCollider)
            {
                boundsFinal = col.bounds;
                encontrouCollider = true;
            }
            else
            {
                boundsFinal.Encapsulate(col.bounds);
            }
        }

        return encontrouCollider;
    }

    private bool EhChao(Collider col)
    {
        if (col == null) return false;
        if (col is TerrainCollider) return true;
        if (col.CompareTag("Terrain")) return true;
        if (col.gameObject.name == "Terrain") return true;
        return false;
    }

    private bool EhBase(Transform alvo)
    {
        if (alvo == null) return false;
        if (alvo.name.Contains("Base")) return true;
        if (alvo.root != null && alvo.root.name.Contains("Base")) return true;
        return false;
    }

    private void DesativarScriptsDaBase(GameObject obj)
    {
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
            if (scripts[i] != null) scripts[i].enabled = false;
    }

    private void AtivarScriptsDaBase(GameObject obj)
    {
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
            if (scripts[i] != null) scripts[i].enabled = true;
    }

    private void OnValidate()
    {
        velocidadeRotacaoMouse = Mathf.Max(0f, velocidadeRotacaoMouse);
        margemColisao = Mathf.Max(0.01f, margemColisao);
    }

    private void OnDrawGizmos()
    {
        GameObject baseAtual = ObterBaseAtual();

        if (baseAtual == null) return;
        if (!TentarCalcularBoundsDaBase(baseAtual, out Bounds boundsBase)) return;

        Gizmos.color = podeConstruir ? Color.green : Color.red;
        Gizmos.DrawWireCube(boundsBase.center, boundsBase.size * margemColisao);
    }
}