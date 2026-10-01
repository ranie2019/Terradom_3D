using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class BaseSelecionavel : MonoBehaviour
{
    public static BaseSelecionavel BaseAtualSelecionada { get; private set; }

    private const string TagJogador = "Azul";
    private static readonly List<BaseSelecionavel> basesSoldadoJogador = new List<BaseSelecionavel>();
    private static int indiceProximaBaseSoldado;
    private static readonly List<BaseSelecionavel> basesTankJogador = new List<BaseSelecionavel>();
    private static int indiceProximaBaseTank;
    private static readonly List<BaseSelecionavel> basesAereasJogador = new List<BaseSelecionavel>();
    private static int indiceProximaBaseAerea;
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
    private TankSpown tankSpown;
    private AviaoSpown aviaoSpown;
    private Renderer[] renderizadores;
    private Material[][] materiais;
    private Color[][] coresOriginais;
    private string[][] propriedadesCor;

    private void Awake()
    {
        if (cameraPrincipal == null)
            cameraPrincipal = Camera.main;

        soldadoSpown = GetComponent<SoldadoSpown>();
        if (soldadoSpown == null)
            soldadoSpown = GetComponentInParent<SoldadoSpown>();
        tankSpown = GetComponent<TankSpown>();
        if (tankSpown == null)
            tankSpown = GetComponentInParent<TankSpown>();

        aviaoSpown = GetComponent<AviaoSpown>();
        if (aviaoSpown == null)
            aviaoSpown = GetComponentInParent<AviaoSpown>();

        renderizadores = GetComponentsInChildren<Renderer>();

        PrepararMateriais();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarCicloDeBases()
    {
        basesSoldadoJogador.Clear();
        basesTankJogador.Clear();
        basesAereasJogador.Clear();
        indiceProximaBaseSoldado = 0;
        indiceProximaBaseTank = 0;
        indiceProximaBaseAerea = 0;
        BaseAtualSelecionada = null;
    }

    private void OnEnable()
    {
        RegistrarBaseSoldadoDoJogador();
        RegistrarBaseTankDoJogador();
        RegistrarBaseAereaDoJogador();
    }

    private void OnDisable()
    {
        DesregistrarBaseSoldadoDoJogador();
        DesregistrarBaseTankDoJogador();
        DesregistrarBaseAereaDoJogador();
        if (BaseAtualSelecionada == this)
            DesselecionarBaseAtual();
    }

    private void RegistrarBaseSoldadoDoJogador()
    {
        if (soldadoSpown == null || basesSoldadoJogador.Contains(this))
            return;

        Transform raiz = transform.root;
        if (!CompareTag(TagJogador) && (raiz == null || !raiz.CompareTag(TagJogador)))
            return;

        // A ativacao da base confirmada preserva a ordem de construcao.
        basesSoldadoJogador.Add(this);
    }

    private void DesregistrarBaseSoldadoDoJogador()
    {
        int indice = basesSoldadoJogador.IndexOf(this);
        if (indice >= 0)
            RemoverBaseDoCiclo(indice);
    }

    private void RegistrarBaseTankDoJogador()
    {
        if (tankSpown == null || basesTankJogador.Contains(this))
            return;

        Transform raiz = transform.root;
        if (!CompareTag(TagJogador) && (raiz == null || !raiz.CompareTag(TagJogador)))
            return;

        // A ativacao da base confirmada preserva a ordem de construcao.
        basesTankJogador.Add(this);
    }

    private void DesregistrarBaseTankDoJogador()
    {
        int indice = basesTankJogador.IndexOf(this);
        if (indice >= 0)
            RemoverBaseTankDoCiclo(indice);
    }

    private static void RemoverBaseTankDoCiclo(int indice)
    {
        basesTankJogador.RemoveAt(indice);
        if (indice < indiceProximaBaseTank)
            indiceProximaBaseTank--;

        if (basesTankJogador.Count == 0 || indiceProximaBaseTank >= basesTankJogador.Count)
            indiceProximaBaseTank = 0;
    }

    /// <summary>
    /// Seleciona a proxima base de tanques do jogador, em ciclo pela ordem de construcao.
    /// </summary>
    public static BaseSelecionavel SelecionarProximaBaseTank()
    {
        // Recupera bases ativas caso a cena tenha permanecido carregada ao iniciar o jogo.
        BaseSelecionavel[] encontradas = FindObjectsByType<BaseSelecionavel>(FindObjectsSortMode.None);
        for (int i = 0; i < encontradas.Length; i++)
        {
            BaseSelecionavel baseEncontrada = encontradas[i];
            if (baseEncontrada != null && baseEncontrada.isActiveAndEnabled)
                baseEncontrada.RegistrarBaseTankDoJogador();
        }

        for (int i = basesTankJogador.Count - 1; i >= 0; i--)
        {
            BaseSelecionavel baseRegistrada = basesTankJogador[i];
            if (baseRegistrada == null || !baseRegistrada.isActiveAndEnabled || baseRegistrada.tankSpown == null)
            {
                RemoverBaseTankDoCiclo(i);
                continue;
            }

            Transform raiz = baseRegistrada.transform.root;
            if (!baseRegistrada.CompareTag(TagJogador) && (raiz == null || !raiz.CompareTag(TagJogador)))
                RemoverBaseTankDoCiclo(i);
        }

        if (basesTankJogador.Count == 0)
        {
            indiceProximaBaseTank = 0;
            return null;
        }

        if (indiceProximaBaseTank < 0 || indiceProximaBaseTank >= basesTankJogador.Count)
            indiceProximaBaseTank = 0;

        BaseSelecionavel proxima = basesTankJogador[indiceProximaBaseTank];
        indiceProximaBaseTank = (indiceProximaBaseTank + 1) % basesTankJogador.Count;
        proxima.Selecionar();
        return proxima;
    }

    private void RegistrarBaseAereaDoJogador()
    {
        if (aviaoSpown == null || basesAereasJogador.Contains(this))
            return;

        Transform raiz = transform.root;
        if (!CompareTag(TagJogador) && (raiz == null || !raiz.CompareTag(TagJogador)))
            return;

        // A ativacao da base confirmada preserva a ordem de construcao.
        basesAereasJogador.Add(this);
    }

    private void DesregistrarBaseAereaDoJogador()
    {
        int indice = basesAereasJogador.IndexOf(this);
        if (indice >= 0)
            RemoverBaseAereaDoCiclo(indice);
    }

    private static void RemoverBaseAereaDoCiclo(int indice)
    {
        basesAereasJogador.RemoveAt(indice);
        if (indice < indiceProximaBaseAerea)
            indiceProximaBaseAerea--;

        if (basesAereasJogador.Count == 0 || indiceProximaBaseAerea >= basesAereasJogador.Count)
            indiceProximaBaseAerea = 0;
    }

    /// <summary>
    /// Seleciona a proxima base aerea do jogador, em ciclo pela ordem de construcao.
    /// </summary>
    public static BaseSelecionavel SelecionarProximaBaseAerea()
    {
        // Recupera bases ativas caso a cena tenha permanecido carregada ao iniciar o jogo.
        BaseSelecionavel[] encontradas = FindObjectsByType<BaseSelecionavel>(FindObjectsSortMode.None);
        for (int i = 0; i < encontradas.Length; i++)
        {
            BaseSelecionavel baseEncontrada = encontradas[i];
            if (baseEncontrada != null && baseEncontrada.isActiveAndEnabled)
                baseEncontrada.RegistrarBaseAereaDoJogador();
        }

        for (int i = basesAereasJogador.Count - 1; i >= 0; i--)
        {
            BaseSelecionavel baseRegistrada = basesAereasJogador[i];
            if (baseRegistrada == null || !baseRegistrada.isActiveAndEnabled || baseRegistrada.aviaoSpown == null)
            {
                RemoverBaseAereaDoCiclo(i);
                continue;
            }

            Transform raiz = baseRegistrada.transform.root;
            if (!baseRegistrada.CompareTag(TagJogador) && (raiz == null || !raiz.CompareTag(TagJogador)))
                RemoverBaseAereaDoCiclo(i);
        }

        if (basesAereasJogador.Count == 0)
        {
            indiceProximaBaseAerea = 0;
            return null;
        }

        if (indiceProximaBaseAerea < 0 || indiceProximaBaseAerea >= basesAereasJogador.Count)
            indiceProximaBaseAerea = 0;

        BaseSelecionavel proxima = basesAereasJogador[indiceProximaBaseAerea];
        indiceProximaBaseAerea = (indiceProximaBaseAerea + 1) % basesAereasJogador.Count;
        proxima.Selecionar();
        return proxima;
    }

    private static void RemoverBaseDoCiclo(int indice)
    {
        basesSoldadoJogador.RemoveAt(indice);
        if (indice < indiceProximaBaseSoldado)
            indiceProximaBaseSoldado--;

        if (basesSoldadoJogador.Count == 0 || indiceProximaBaseSoldado >= basesSoldadoJogador.Count)
            indiceProximaBaseSoldado = 0;
    }

    /// <summary>
    /// Seleciona a proxima base de soldados do jogador, em ciclo pela ordem de construcao.
    /// </summary>
    public static BaseSelecionavel SelecionarProximaBaseSoldado()
    {
        // Recupera bases ativas caso a cena tenha permanecido carregada ao iniciar o jogo.
        BaseSelecionavel[] encontradas = FindObjectsByType<BaseSelecionavel>(FindObjectsSortMode.None);
        for (int i = 0; i < encontradas.Length; i++)
        {
            BaseSelecionavel baseEncontrada = encontradas[i];
            if (baseEncontrada != null && baseEncontrada.isActiveAndEnabled)
                baseEncontrada.RegistrarBaseSoldadoDoJogador();
        }

        for (int i = basesSoldadoJogador.Count - 1; i >= 0; i--)
        {
            BaseSelecionavel baseRegistrada = basesSoldadoJogador[i];
            if (baseRegistrada == null || !baseRegistrada.isActiveAndEnabled || baseRegistrada.soldadoSpown == null)
            {
                RemoverBaseDoCiclo(i);
                continue;
            }

            Transform raiz = baseRegistrada.transform.root;
            if (!baseRegistrada.CompareTag(TagJogador) && (raiz == null || !raiz.CompareTag(TagJogador)))
                RemoverBaseDoCiclo(i);
        }

        if (basesSoldadoJogador.Count == 0)
        {
            indiceProximaBaseSoldado = 0;
            return null;
        }

        if (indiceProximaBaseSoldado < 0 || indiceProximaBaseSoldado >= basesSoldadoJogador.Count)
            indiceProximaBaseSoldado = 0;

        BaseSelecionavel proxima = basesSoldadoJogador[indiceProximaBaseSoldado];
        indiceProximaBaseSoldado = (indiceProximaBaseSoldado + 1) % basesSoldadoJogador.Count;
        proxima.Selecionar();
        return proxima;
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
        DesregistrarBaseSoldadoDoJogador();
        DesregistrarBaseTankDoJogador();
        DesregistrarBaseAereaDoJogador();
        RestaurarCoresOriginais();

        if (BaseAtualSelecionada == this)
        {
            BaseAtualSelecionada = null;
            BotoesProducaoUnidades.AtualizarTodos();
        }
    }
}