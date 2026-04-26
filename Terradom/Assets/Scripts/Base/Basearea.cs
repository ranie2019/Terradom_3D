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

    private void Awake()
    {
        if (cameraPrincipal == null)
            cameraPrincipal = Camera.main;
    }

    private void Update()
    {
        if (!estaPosicionando || baseAtual == null)
            return;

        bool botaoDireitoPressionado =
            Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (botaoDireitoPressionado)
            RotacionarComBotaoDireito();
        else
            AtualizarPosicaoComMouse();

        VerificarColisao();
        ConfirmarComBotaoEsquerdo();
    }

    public void CriarBaseParaPosicionar()
    {
        if (prefabBase == null)
        {
            Debug.LogWarning("Prefab da base não foi colocado no BaseArea.");
            return;
        }

        if (baseAtual != null)
        {
            if (Application.isPlaying)
                Destroy(baseAtual);
            else
                DestroyImmediate(baseAtual);
        }

        baseAtual = Instantiate(prefabBase);
        estaPosicionando = true;
        podeConstruir = false;

        DesativarScriptsDaBase(baseAtual);
    }

    private void AtualizarPosicaoComMouse()
    {
        if (Mouse.current == null || cameraPrincipal == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cameraPrincipal.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, camadaDoChao, QueryTriggerInteraction.Ignore))
        {
            Vector3 novaPos = hit.point;
            novaPos.y = 0f;
            baseAtual.transform.position = novaPos;
        }
    }

    private void RotacionarComBotaoDireito()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.rightButton.isPressed)
            return;

        Vector2 deltaMouse = Mouse.current.delta.ReadValue();
        float rotacaoY = deltaMouse.y * velocidadeRotacaoMouse;

        baseAtual.transform.Rotate(Vector3.up, rotacaoY, Space.World);
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
        {
            Debug.Log("Não pode colocar a base aqui. Existe outro objeto ocupando essa área.");
            return;
        }

        TravarBase();
    }

    private void TravarBase()
    {
        estaPosicionando = false;
        podeConstruir = true;

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

            if (!bloquearQualquerObjeto && !EhBase(col.transform))
                continue;

            podeConstruir = false;
            return;
        }
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