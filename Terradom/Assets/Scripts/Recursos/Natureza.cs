using UnityEngine;

[DisallowMultipleComponent]
public class Natureza : MonoBehaviour
{
    private enum TipoRecursoSpawn
    {
        Arvore,
        Pedra,
        Metal
    }

    [Header("Terreno onde vai spawnar")]
    [SerializeField] private Terrain terrain;

    [Header("Organizacao na Hierarchy")]
    [SerializeField] private Transform pastaClones;
    [SerializeField] private string nomePastaClones = "Clone";
    [SerializeField] private string nomeObjetoRecursos = "Recursos";
    [SerializeField] private bool criarPastaCloneSeNaoExistir = true;
    [SerializeField] private bool manterClonesDentroDaPasta = true;

    [Header("Arvore")]
    [SerializeField] private GameObject[] prefabsArvore;
    [SerializeField] private string tagArvore = "Arvore";
    [SerializeField] private float ajusteYArvore = 0f;

    [Header("Pedra")]
    [SerializeField] private GameObject[] prefabsPedra;
    [SerializeField] private string tagPedra = "Pedra";
    [SerializeField] private float ajusteYPedra = 1f;

    [Header("Metal")]
    [SerializeField] private GameObject[] prefabsMetal;
    [SerializeField] private string tagMetal = "Metal";
    [SerializeField] private float ajusteYMetal = 1f;

    [Header("Ordem balanceada")]
    [SerializeField] private TipoRecursoSpawn proximoTipoParaSpawn = TipoRecursoSpawn.Arvore;
    [SerializeField] private bool seguirOrdemBalanceada = true;
    [SerializeField] private bool forcarTagNoCloneCriado = true;

    [Header("Tempo")]
    [SerializeField] private float tempoEntreSpawns = 5f;

    [Header("Espacamento")]
    [SerializeField] private float distanciaMinimaEntreObjetos = 40f;
    [SerializeField] private LayerMask camadasComColisao = ~0;

    [Header("Tentativas por ciclo")]
    [SerializeField] private int tentativasPorSpawn = 20;

    [Header("Compatibilidade - lista antiga")]
    [SerializeField] private GameObject[] prefabsNatureza;

    private float proximoSpawn;

    private void Awake()
    {
        PrepararPastaDeClones();
        proximoSpawn = Time.time + tempoEntreSpawns;
    }

    private void Update()
    {
        if (Time.time < proximoSpawn)
            return;

        proximoSpawn = Time.time + tempoEntreSpawns;
        TentarSpawnarObjeto();
    }

    private void TentarSpawnarObjeto()
    {
        if (terrain == null)
            return;

        if (seguirOrdemBalanceada)
        {
            TentarSpawnarObjetoBalanceado();
            return;
        }

        TentarSpawnarObjetoAleatorioAntigo();
    }

    private void TentarSpawnarObjetoBalanceado()
    {
        if (!ExisteAlgumPrefabConfigurado())
            return;

        TipoRecursoSpawn tipoEscolhido = proximoTipoParaSpawn;

        for (int tentativaTipo = 0; tentativaTipo < 3; tentativaTipo++)
        {
            if (ExistePrefabParaTipo(tipoEscolhido))
            {
                bool criou = TentarSpawnarTipo(tipoEscolhido);

                if (criou)
                    AvancarTipoParaProximoSpawn();

                return;
            }

            tipoEscolhido = ObterProximoTipo(tipoEscolhido);
            proximoTipoParaSpawn = tipoEscolhido;
        }
    }

    private bool TentarSpawnarTipo(TipoRecursoSpawn tipo)
    {
        for (int i = 0; i < tentativasPorSpawn; i++)
        {
            GameObject prefab = EscolherPrefabAleatorioPorTipo(tipo);

            if (prefab == null)
                continue;

            Vector3 posicao = GerarPosicaoAleatoriaNoTerrain(ObterAjusteYPorTipo(tipo));

            if (!TemEspacoLivre(posicao))
                continue;

            CriarClone(prefab, posicao, tipo);
            return true;
        }

        return false;
    }

    private void TentarSpawnarObjetoAleatorioAntigo()
    {
        if (prefabsNatureza == null || prefabsNatureza.Length == 0)
            return;

        for (int i = 0; i < tentativasPorSpawn; i++)
        {
            GameObject prefab = EscolherPrefabAleatorioDaLista(prefabsNatureza);

            if (prefab == null)
                continue;

            Vector3 posicao = GerarPosicaoAleatoriaNoTerrain(0f);

            if (!TemEspacoLivre(posicao))
                continue;

            GameObject novo = Instantiate(prefab, posicao, Quaternion.identity);
            novo.name = prefab.name;
            ColocarCloneNaPasta(novo);
            return;
        }
    }

    private void CriarClone(GameObject prefab, Vector3 posicao, TipoRecursoSpawn tipo)
    {
        GameObject novo = Instantiate(prefab, posicao, Quaternion.identity);
        novo.name = prefab.name;

        if (forcarTagNoCloneCriado)
            DefinirTagComSeguranca(novo, ObterTagPorTipo(tipo));

        ColocarCloneNaPasta(novo);
    }

    private void ColocarCloneNaPasta(GameObject novo)
    {
        if (!manterClonesDentroDaPasta || novo == null)
            return;

        if (pastaClones == null)
            PrepararPastaDeClones();

        if (pastaClones != null)
            novo.transform.SetParent(pastaClones, true);
    }

    private void PrepararPastaDeClones()
    {
        if (!manterClonesDentroDaPasta)
            return;

        if (pastaClones != null)
            return;

        if (!string.IsNullOrWhiteSpace(nomeObjetoRecursos) && !string.IsNullOrWhiteSpace(nomePastaClones))
        {
            GameObject pastaPorCaminho = GameObject.Find(nomeObjetoRecursos + "/" + nomePastaClones);

            if (pastaPorCaminho != null)
            {
                pastaClones = pastaPorCaminho.transform;
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(nomePastaClones))
        {
            GameObject pastaExistente = GameObject.Find(nomePastaClones);

            if (pastaExistente != null)
            {
                pastaClones = pastaExistente.transform;
                return;
            }
        }

        if (!criarPastaCloneSeNaoExistir)
            return;

        Transform pai = null;

        if (!string.IsNullOrWhiteSpace(nomeObjetoRecursos))
        {
            GameObject objetoRecursos = GameObject.Find(nomeObjetoRecursos);

            if (objetoRecursos != null)
                pai = objetoRecursos.transform;
        }

        if (pai == null)
            pai = transform;

        GameObject novaPasta = new GameObject(string.IsNullOrWhiteSpace(nomePastaClones) ? "Clone" : nomePastaClones);
        novaPasta.transform.SetParent(pai, false);
        novaPasta.transform.localPosition = Vector3.zero;
        novaPasta.transform.localRotation = Quaternion.identity;
        novaPasta.transform.localScale = Vector3.one;

        pastaClones = novaPasta.transform;
    }

    private GameObject EscolherPrefabAleatorioPorTipo(TipoRecursoSpawn tipo)
    {
        GameObject[] listaPrincipal = ObterListaPrincipalPorTipo(tipo);

        if (ListaTemPrefab(listaPrincipal))
            return EscolherPrefabAleatorioDaLista(listaPrincipal);

        GameObject prefabDaListaAntiga = EscolherPrefabAntigoPorTag(ObterTagPorTipo(tipo));

        if (prefabDaListaAntiga != null)
            return prefabDaListaAntiga;

        return null;
    }

    private GameObject EscolherPrefabAleatorioDaLista(GameObject[] lista)
    {
        if (lista == null || lista.Length == 0)
            return null;

        int quantidadeValidos = 0;

        for (int i = 0; i < lista.Length; i++)
        {
            if (lista[i] != null)
                quantidadeValidos++;
        }

        if (quantidadeValidos <= 0)
            return null;

        int escolhido = UnityEngine.Random.Range(0, quantidadeValidos);
        int contador = 0;

        for (int i = 0; i < lista.Length; i++)
        {
            if (lista[i] == null)
                continue;

            if (contador == escolhido)
                return lista[i];

            contador++;
        }

        return null;
    }

    private GameObject EscolherPrefabAntigoPorTag(string tagProcurada)
    {
        if (prefabsNatureza == null || prefabsNatureza.Length == 0)
            return null;

        int quantidadeValidos = 0;

        for (int i = 0; i < prefabsNatureza.Length; i++)
        {
            GameObject prefab = prefabsNatureza[i];

            if (prefab == null)
                continue;

            if (TemTagComSeguranca(prefab, tagProcurada))
                quantidadeValidos++;
        }

        if (quantidadeValidos <= 0)
            return null;

        int escolhido = UnityEngine.Random.Range(0, quantidadeValidos);
        int contador = 0;

        for (int i = 0; i < prefabsNatureza.Length; i++)
        {
            GameObject prefab = prefabsNatureza[i];

            if (prefab == null)
                continue;

            if (!TemTagComSeguranca(prefab, tagProcurada))
                continue;

            if (contador == escolhido)
                return prefab;

            contador++;
        }

        return null;
    }

    private Vector3 GerarPosicaoAleatoriaNoTerrain(float ajusteY)
    {
        TerrainData data = terrain.terrainData;
        Vector3 origem = terrain.transform.position;
        Vector3 tamanho = data.size;

        float x = UnityEngine.Random.Range(origem.x, origem.x + tamanho.x);
        float z = UnityEngine.Random.Range(origem.z, origem.z + tamanho.z);
        float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + origem.y + ajusteY;

        return new Vector3(x, y, z);
    }

    private bool TemEspacoLivre(Vector3 posicao)
    {
        Vector3 centro = new Vector3(posicao.x, posicao.y + 2f, posicao.z);

        Collider[] colisores = Physics.OverlapSphere(
            centro,
            distanciaMinimaEntreObjetos,
            camadasComColisao,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < colisores.Length; i++)
        {
            Collider col = colisores[i];

            if (col == null)
                continue;

            if (col is TerrainCollider)
                continue;

            Vector3 posCol = col.transform.position;

            float dx = posCol.x - posicao.x;
            float dz = posCol.z - posicao.z;

            float distanciaXZ = Mathf.Sqrt(dx * dx + dz * dz);

            if (distanciaXZ < distanciaMinimaEntreObjetos)
                return false;
        }

        return true;
    }

    private bool ExisteAlgumPrefabConfigurado()
    {
        if (ListaTemPrefab(prefabsArvore))
            return true;

        if (ListaTemPrefab(prefabsPedra))
            return true;

        if (ListaTemPrefab(prefabsMetal))
            return true;

        if (ListaTemPrefab(prefabsNatureza))
            return true;

        return false;
    }

    private bool ExistePrefabParaTipo(TipoRecursoSpawn tipo)
    {
        if (ListaTemPrefab(ObterListaPrincipalPorTipo(tipo)))
            return true;

        if (EscolherPrefabAntigoPorTag(ObterTagPorTipo(tipo)) != null)
            return true;

        return false;
    }

    private bool ListaTemPrefab(GameObject[] lista)
    {
        if (lista == null || lista.Length == 0)
            return false;

        for (int i = 0; i < lista.Length; i++)
        {
            if (lista[i] != null)
                return true;
        }

        return false;
    }

    private GameObject[] ObterListaPrincipalPorTipo(TipoRecursoSpawn tipo)
    {
        switch (tipo)
        {
            case TipoRecursoSpawn.Pedra:
                return prefabsPedra;

            case TipoRecursoSpawn.Metal:
                return prefabsMetal;

            default:
                return prefabsArvore;
        }
    }

    private string ObterTagPorTipo(TipoRecursoSpawn tipo)
    {
        switch (tipo)
        {
            case TipoRecursoSpawn.Pedra:
                return tagPedra;

            case TipoRecursoSpawn.Metal:
                return tagMetal;

            default:
                return tagArvore;
        }
    }

    private float ObterAjusteYPorTipo(TipoRecursoSpawn tipo)
    {
        switch (tipo)
        {
            case TipoRecursoSpawn.Pedra:
                return ajusteYPedra;

            case TipoRecursoSpawn.Metal:
                return ajusteYMetal;

            default:
                return ajusteYArvore;
        }
    }

    private TipoRecursoSpawn ObterProximoTipo(TipoRecursoSpawn tipoAtual)
    {
        switch (tipoAtual)
        {
            case TipoRecursoSpawn.Arvore:
                return TipoRecursoSpawn.Pedra;

            case TipoRecursoSpawn.Pedra:
                return TipoRecursoSpawn.Metal;

            default:
                return TipoRecursoSpawn.Arvore;
        }
    }

    private void AvancarTipoParaProximoSpawn()
    {
        proximoTipoParaSpawn = ObterProximoTipo(proximoTipoParaSpawn);
    }

    private bool TemTagComSeguranca(GameObject obj, string tagProcurada)
    {
        if (obj == null || string.IsNullOrWhiteSpace(tagProcurada))
            return false;

        try
        {
            return obj.CompareTag(tagProcurada);
        }
        catch
        {
            return false;
        }
    }

    private void DefinirTagComSeguranca(GameObject obj, string novaTag)
    {
        if (obj == null || string.IsNullOrWhiteSpace(novaTag))
            return;

        try
        {
            obj.tag = novaTag;
        }
        catch
        {
            // Se a tag nao existir no Unity, mantem a tag original do prefab.
        }
    }

    private void OnValidate()
    {
        tempoEntreSpawns = Mathf.Max(0.1f, tempoEntreSpawns);
        distanciaMinimaEntreObjetos = Mathf.Max(0f, distanciaMinimaEntreObjetos);
        tentativasPorSpawn = Mathf.Max(1, tentativasPorSpawn);

        ajusteYArvore = Mathf.Max(0f, ajusteYArvore);
        ajusteYPedra = Mathf.Max(0f, ajusteYPedra);
        ajusteYMetal = Mathf.Max(0f, ajusteYMetal);
    }
}
