using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class BaseSelecionavel : MonoBehaviour
{
    public static BaseSelecionavel BaseAtualSelecionada { get; private set; }

    [Header("Seleção")]
    [SerializeField] private Camera cameraPrincipal;
    [SerializeField] private LayerMask camadaSelecionavel = ~0;

    [Header("Debug")]
    [SerializeField] private bool debugSelecionar = true;

    [Header("Visual da seleção")]
    [SerializeField] private bool piscarQuandoSelecionada = true;
    [SerializeField] private Color corSelecionada = new Color(1f, 0.65f, 0.25f, 1f);
    [SerializeField] private float velocidadePiscar = 4f;

    private SoldadoSpown soldadoSpown;
    private Renderer[] renderizadores;
    private Material[][] materiais;
    private Color[][] coresOriginais;
    private string[][] propriedadesCor;

    private void Awake()
    {
        if (cameraPrincipal == null)
            cameraPrincipal = Camera.main;

        soldadoSpown = GetComponent<SoldadoSpown>();
        renderizadores = GetComponentsInChildren<Renderer>();

        PrepararMateriais();
    }

    private void Update()
    {
        VerificarClique();

        if (BaseAtualSelecionada == this && piscarQuandoSelecionada)
            AtualizarPiscar();
        else
            RestaurarCoresOriginais();
    }

    private void VerificarClique()
    {
        if (Mouse.current == null || cameraPrincipal == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cameraPrincipal.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, camadaSelecionavel, QueryTriggerInteraction.Ignore))
        {
            BaseSelecionavel baseClicada = hit.transform.GetComponentInParent<BaseSelecionavel>();

            if (baseClicada != null)
            {
                baseClicada.Selecionar();
                return;
            }

            DesselecionarBaseAtual();
        }
        else
        {
            DesselecionarBaseAtual();
        }
    }

    public void Selecionar()
    {
        if (BaseAtualSelecionada != null && BaseAtualSelecionada != this)
            BaseAtualSelecionada.RestaurarCoresOriginais();

        BaseAtualSelecionada = this;

        if (debugSelecionar)
            Debug.Log("Base selecionada: " + gameObject.name);

        BotoesProducaoUnidades.AtualizarTodos();
    }

    public static void DesselecionarBaseAtual()
    {
        if (BaseAtualSelecionada != null)
        {
            BaseAtualSelecionada.RestaurarCoresOriginais();

            if (BaseAtualSelecionada.debugSelecionar)
                Debug.Log("Base desselecionada: " + BaseAtualSelecionada.gameObject.name);
        }

        BaseAtualSelecionada = null;
        BotoesProducaoUnidades.AtualizarTodos();
    }

    public SoldadoSpown GetSoldadoSpown()
    {
        return soldadoSpown;
    }

    private void PrepararMateriais()
    {
        materiais = new Material[renderizadores.Length][];
        coresOriginais = new Color[renderizadores.Length][];
        propriedadesCor = new string[renderizadores.Length][];

        for (int i = 0; i < renderizadores.Length; i++)
        {
            materiais[i] = renderizadores[i].materials;
            coresOriginais[i] = new Color[materiais[i].Length];
            propriedadesCor[i] = new string[materiais[i].Length];

            for (int j = 0; j < materiais[i].Length; j++)
            {
                Material mat = materiais[i][j];

                if (mat == null)
                    continue;

                string prop = ObterPropriedadeDeCor(mat);
                propriedadesCor[i][j] = prop;

                if (!string.IsNullOrEmpty(prop))
                    coresOriginais[i][j] = mat.GetColor(prop);
            }
        }
    }

    private string ObterPropriedadeDeCor(Material mat)
    {
        if (mat.HasProperty("_BaseColor"))
            return "_BaseColor";

        if (mat.HasProperty("_Color"))
            return "_Color";

        return "";
    }

    private void AtualizarPiscar()
    {
        float intensidade = Mathf.PingPong(Time.time * velocidadePiscar, 1f);

        for (int i = 0; i < materiais.Length; i++)
        {
            for (int j = 0; j < materiais[i].Length; j++)
            {
                Material mat = materiais[i][j];
                string prop = propriedadesCor[i][j];

                if (mat == null || string.IsNullOrEmpty(prop))
                    continue;

                Color corFinal = Color.Lerp(coresOriginais[i][j], corSelecionada, intensidade);
                mat.SetColor(prop, corFinal);
            }
        }
    }

    private void RestaurarCoresOriginais()
    {
        if (materiais == null || coresOriginais == null)
            return;

        for (int i = 0; i < materiais.Length; i++)
        {
            for (int j = 0; j < materiais[i].Length; j++)
            {
                Material mat = materiais[i][j];
                string prop = propriedadesCor[i][j];

                if (mat == null || string.IsNullOrEmpty(prop))
                    continue;

                mat.SetColor(prop, coresOriginais[i][j]);
            }
        }
    }

    private void OnDestroy()
    {
        RestaurarCoresOriginais();

        if (BaseAtualSelecionada == this)
        {
            BaseAtualSelecionada = null;
            BotoesProducaoUnidades.AtualizarTodos();
        }
    }
}