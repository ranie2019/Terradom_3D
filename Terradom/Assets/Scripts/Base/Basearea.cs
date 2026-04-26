using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class BaseArea : MonoBehaviour
{
    [Header("Prefab da Base")]
    [SerializeField] private GameObject prefabBase;

    [Header("Referências")]
    [SerializeField] private Camera cameraPrincipal;
    [SerializeField] private LayerMask camadaDoChao = ~0;
    [SerializeField] private LayerMask camadaBloqueio = ~0;

    [Header("Rotação com botão direito")]
    [SerializeField] private float velocidadeRotacaoMouse = 0.35f;

    [Header("Colisão")]
    [SerializeField] private float margemColisao = 0.95f;
    [SerializeField] private bool bloquearQualquerObjeto = true;

    [Header("Estado Atual")]
    [SerializeField] private GameObject baseAtual;
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
        if (!estaPosicionando || baseAtual == null)
            return;

        AlternarModoComBotaoDireito();

        if (modoRotacao)
            RotacionarComMouse();
        else
            AtualizarPosicaoComMouse();

        VerificarColisao();
        ConfirmarComBotaoEsquerdo();
    }

    public void CriarBaseParaPosicionar()
    {
        if (prefabBase == null)
            return;

        if (estaPosicionando)
            return;

        if (GameControllerRecursos.Instance == null)
            return;

        if (!GameControllerRecursos.Instance.TentarGastarRecursosDaBase())
            return;

        if (baseAtual != null)
            Destroy(baseAtual);

        baseAtual = Instantiate(prefabBase);
        baseAtual.SetActive(true);

        estaPosicionando = true;
        podeConstruir = false;
        modoRotacao = false;

        DesativarScriptsDaBase(baseAtual);
        PosicionarBaseNoCentroDaTela();
        VerificarColisao();
    }

    private void AlternarModoComBotaoDireito()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        modoRotacao = !modoRotacao;
    }

    private void AtualizarPosicaoComMouse()
    {
        if (Mouse.current == null || cameraPrincipal == null || baseAtual == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (TentarPegarPontoNoChao(mousePos, out Vector3 ponto))
        {
            ponto.y = 0f;
            baseAtual.transform.position = ponto;
        }
    }

    private void RotacionarComMouse()
    {
        if (Mouse.current == null || baseAtual == null)
            return;

        Vector2 deltaMouse = Mouse.current.delta.ReadValue();

        float rotacaoY = deltaMouse.y * velocidadeRotacaoMouse;

        baseAtual.transform.Rotate(Vector3.up, rotacaoY, Space.World);
    }

    private void PosicionarBaseNoCentroDaTela()
    {
        if (cameraPrincipal == null || baseAtual == null)
            return;

        Vector2 centroTela = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (TentarPegarPontoNoChao(centroTela, out Vector3 ponto))
        {
            ponto.y = 0f;
            baseAtual.transform.position = ponto;
        }
    }

    private bool TentarPegarPontoNoChao(Vector2 posicaoTela, out Vector3 ponto)
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
        if (baseAtual == null)
            return;

        estaPosicionando = false;
        podeConstruir = true;
        modoRotacao = false;

        AtivarScriptsDaBase(baseAtual);
        baseAtual = null;
    }

    private void VerificarColisao()
    {
        if (baseAtual == null)
        {
            podeConstruir = false;
            return;
        }

        Collider baseCollider = baseAtual.GetComponent<Collider>();

        if (baseCollider == null)
            baseCollider = baseAtual.GetComponentInChildren<Collider>();

        if (baseCollider == null)
        {
            podeConstruir = true;
            return;
        }

        Bounds bounds = baseCollider.bounds;

        Collider[] colisores = Physics.OverlapBox(
            bounds.center,
            bounds.extents * margemColisao,
            baseAtual.transform.rotation,
            camadaBloqueio,
            QueryTriggerInteraction.Ignore
        );

        podeConstruir = true;

        for (int i = 0; i < colisores.Length; i++)
        {
            Collider col = colisores[i];

            if (col == null)
                continue;

            if (col.transform.IsChildOf(baseAtual.transform))
                continue;

            if (col.gameObject == baseAtual)
                continue;

            if (EhChao(col))
                continue;

            if (!bloquearQualquerObjeto && !EhBase(col.transform))
                continue;

            podeConstruir = false;
            return;
        }
    }

    private bool EhChao(Collider col)
    {
        if (col == null)
            return false;

        if (col is TerrainCollider)
            return true;

        if (col.gameObject.name == "Terrain")
            return true;

        return false;
    }

    private bool EhBase(Transform alvo)
    {
        if (alvo == null)
            return false;

        if (alvo.name.Contains("Base"))
            return true;

        if (alvo.root != null && alvo.root.name.Contains("Base"))
            return true;

        return false;
    }

    private void DesativarScriptsDaBase(GameObject obj)
    {
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();

        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null)
                scripts[i].enabled = false;
        }
    }

    private void AtivarScriptsDaBase(GameObject obj)
    {
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();

        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null)
                scripts[i].enabled = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (baseAtual == null)
            return;

        Collider baseCollider = baseAtual.GetComponent<Collider>();

        if (baseCollider == null)
            baseCollider = baseAtual.GetComponentInChildren<Collider>();

        if (baseCollider == null)
            return;

        Gizmos.color = podeConstruir ? Color.green : Color.red;

        Gizmos.DrawWireCube(
            baseCollider.bounds.center,
            baseCollider.bounds.size * margemColisao
        );
    }
}