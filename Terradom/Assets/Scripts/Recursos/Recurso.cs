using UnityEngine;

[DisallowMultipleComponent]
public class Recurso : MonoBehaviour
{
    [Header("Quantidade")]
    [SerializeField] private int valorMaximo = 300;
    [SerializeField] private int valorAtual = 300;

    [Header("Coleta / Dano")]
    [SerializeField] private int valorPorColeta = 1;
    [SerializeField] private float intervaloEntreColetas = 0.25f;

    [Header("Ferramentas")]
    [SerializeField] private string tagPicareta = "Picareta";
    [SerializeField] private string tagMachado = "Machado";

    [Header("Jogadores")]
    [SerializeField] private string[] tagsDosDonos = { "Azul", "Vermelho" };

    [Header("Colisores filhos")]
    [SerializeField] private bool detectarColisoresNosFilhos = true;
    [SerializeField] private bool adicionarSensorNosFilhosAutomaticamente = true;

    [Header("Debug")]
    [SerializeField] private bool mostrarLogsColeta = false;

    private float proximaColeta = 0f;

    private void Awake()
    {
        valorAtual = valorMaximo;

        if (detectarColisoresNosFilhos && adicionarSensorNosFilhosAutomaticamente)
            RegistrarColisoresFilhos();
    }

    private void OnEnable()
    {
        if (detectarColisoresNosFilhos && adicionarSensorNosFilhosAutomaticamente)
            RegistrarColisoresFilhos();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        TentarColetar(collision.collider.transform);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        TentarColetar(collision.collider.transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        TentarColetar(other.transform);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null)
            return;

        TentarColetar(other.transform);
    }

    public void ReceberColisaoDoFilho(Collision collision)
    {
        if (!detectarColisoresNosFilhos)
            return;

        if (collision == null || collision.collider == null)
            return;

        TentarColetar(collision.collider.transform);
    }

    public void ReceberTriggerDoFilho(Collider other)
    {
        if (!detectarColisoresNosFilhos)
            return;

        if (other == null)
            return;

        TentarColetar(other.transform);
    }

    private void RegistrarColisoresFilhos()
    {
        Collider[] colisores = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colisores.Length; i++)
        {
            Collider colisor = colisores[i];

            if (colisor == null)
                continue;

            if (colisor.gameObject == gameObject)
                continue;

            RecursoColliderFilho sensor = colisor.GetComponent<RecursoColliderFilho>();

            if (sensor == null)
                sensor = colisor.gameObject.AddComponent<RecursoColliderFilho>();

            sensor.DefinirRecursoPai(this);
        }
    }

    private void TentarColetar(Transform objetoQueTocou)
    {
        if (objetoQueTocou == null)
            return;

        if (Time.time < proximaColeta)
            return;

        Transform ferramenta = EncontrarFerramenta(objetoQueTocou);
        if (ferramenta == null)
            return;

        string tipoRecurso = ObterTipoRecurso();

        if (string.IsNullOrWhiteSpace(tipoRecurso))
            return;

        if (!FerramentaPodeColetar(ferramenta, tipoRecurso))
            return;

        string tagDono = EncontrarTagDono(ferramenta);
        if (string.IsNullOrWhiteSpace(tagDono))
            return;

        AplicarColeta(tagDono, tipoRecurso, valorPorColeta);
    }

    private void AplicarColeta(string tagDono, string tipoRecurso, int quantidade)
    {
        if (quantidade <= 0)
            return;

        proximaColeta = Time.time + intervaloEntreColetas;
        valorAtual -= quantidade;

        // Adiciona recursos ao controlador do time correto
        AdicionarRecursoAoControlador(tagDono, tipoRecurso, quantidade);

        if (mostrarLogsColeta)
            Debug.Log($"[Recurso] {tagDono} coletou {quantidade} de {tipoRecurso}. Restante: {valorAtual}/{valorMaximo}");

        if (valorAtual <= 0)
        {
            valorAtual = 0;

            if (mostrarLogsColeta)
                Debug.Log($"[Recurso] {gameObject.name} esgotado e será destruído.");

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adiciona recursos ao controlador correto baseado na tag do dono.
    /// Suporta múltiplos times. Basta adicionar novos casos conforme novas tags forem criadas.
    /// </summary>
    private void AdicionarRecursoAoControlador(string tagDono, string tipoRecurso, int quantidade)
    {
        if (string.IsNullOrWhiteSpace(tagDono) || quantidade <= 0)
            return;

        // Time Azul - Jogador
        if (tagDono == "Azul")
        {
            if (GameControllerRecursos.Instance != null)
            {
                GameControllerRecursos.Instance.AdicionarRecurso(tagDono, tipoRecurso, quantidade);
            }
            else if (mostrarLogsColeta)
            {
                Debug.LogWarning($"[Recurso] GameControllerRecursos.Instance é null! Recurso não foi adicionado para {tagDono}.");
            }
            return;
        }

        // Time Vermelho - IA
        if (tagDono == "Vermelho")
        {
            if (GameControllerRecursosIA.Instance != null)
            {
                GameControllerRecursosIA.Instance.AdicionarRecurso(tagDono, tipoRecurso, quantidade);
            }
            else if (mostrarLogsColeta)
            {
                Debug.LogWarning($"[Recurso] GameControllerRecursosIA.Instance é null! Recurso não foi adicionado para {tagDono}.");
            }
            return;
        }

        // =====================================================================
        // NOVOS TIMES NO FUTURO - ADICIONE AQUI
        // =====================================================================
        // Exemplo:
        //
        // if (tagDono == "Verde")
        // {
        //     if (GameControllerRecursosVerde.Instance != null)
        //     {
        //         GameControllerRecursosVerde.Instance.AdicionarRecurso(tagDono, tipoRecurso, quantidade);
        //     }
        //     return;
        // }
        //
        // if (tagDono == "Amarelo")
        // {
        //     if (GameControllerRecursosAmarelo.Instance != null)
        //     {
        //         GameControllerRecursosAmarelo.Instance.AdicionarRecurso(tagDono, tipoRecurso, quantidade);
        //     }
        //     return;
        // }
        // =====================================================================

        // Se nenhum controlador foi encontrado para esta tag
        if (mostrarLogsColeta)
            Debug.LogWarning($"[Recurso] Nenhum controlador de recursos configurado para a tag '{tagDono}'. Recurso não foi adicionado.");
    }

    private string ObterTipoRecurso()
    {
        Transform atual = transform;

        while (atual != null)
        {
            if (atual.CompareTag("Pedra"))
                return "Pedra";

            if (atual.CompareTag("Arvore"))
                return "Arvore";

            if (atual.CompareTag("Metal"))
                return "Metal";

            atual = atual.parent;
        }

        Transform recursoNosFilhos = EncontrarTagNosFilhos(transform, "Pedra");
        if (recursoNosFilhos != null)
            return "Pedra";

        recursoNosFilhos = EncontrarTagNosFilhos(transform, "Arvore");
        if (recursoNosFilhos != null)
            return "Arvore";

        recursoNosFilhos = EncontrarTagNosFilhos(transform, "Metal");
        if (recursoNosFilhos != null)
            return "Metal";

        return "";
    }

    private Transform EncontrarTagNosFilhos(Transform raiz, string tagProcurada)
    {
        if (raiz == null || string.IsNullOrWhiteSpace(tagProcurada))
            return null;

        Transform[] filhos = raiz.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < filhos.Length; i++)
        {
            Transform filho = filhos[i];

            if (filho != null && filho.CompareTag(tagProcurada))
                return filho;
        }

        return null;
    }

    private Transform EncontrarFerramenta(Transform origem)
    {
        Transform atual = origem;

        while (atual != null)
        {
            if (atual.CompareTag(tagPicareta) || atual.CompareTag(tagMachado))
                return atual;

            atual = atual.parent;
        }

        return null;
    }

    private bool FerramentaPodeColetar(Transform ferramenta, string tipoRecurso)
    {
        if (ferramenta == null || string.IsNullOrWhiteSpace(tipoRecurso))
            return false;

        if (ferramenta.CompareTag(tagPicareta) && tipoRecurso == "Pedra")
            return true;

        if (ferramenta.CompareTag(tagPicareta) && tipoRecurso == "Metal")
            return true;

        if (ferramenta.CompareTag(tagMachado) && tipoRecurso == "Arvore")
            return true;

        return false;
    }

    private string EncontrarTagDono(Transform ferramenta)
    {
        Transform atual = ferramenta;

        while (atual != null)
        {
            for (int i = 0; i < tagsDosDonos.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(tagsDosDonos[i]) && atual.CompareTag(tagsDosDonos[i]))
                    return tagsDosDonos[i];
            }

            atual = atual.parent;
        }

        return "";
    }

    public int GetValorAtual()
    {
        return valorAtual;
    }

    public int GetValorMaximo()
    {
        return valorMaximo;
    }

    public void SetValorAtual(int novoValor)
    {
        valorAtual = Mathf.Clamp(novoValor, 0, valorMaximo);
    }

    private void OnValidate()
    {
        valorMaximo = Mathf.Max(1, valorMaximo);
        valorAtual = Mathf.Clamp(valorAtual, 0, valorMaximo);
        valorPorColeta = Mathf.Max(1, valorPorColeta);
        intervaloEntreColetas = Mathf.Max(0f, intervaloEntreColetas);
    }
}

[DisallowMultipleComponent]
public class RecursoColliderFilho : MonoBehaviour
{
    [SerializeField] private Recurso recursoPai;

    public void DefinirRecursoPai(Recurso novoRecursoPai)
    {
        recursoPai = novoRecursoPai;
    }

    private void Awake()
    {
        if (recursoPai == null)
            recursoPai = GetComponentInParent<Recurso>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (recursoPai == null)
            return;

        recursoPai.ReceberColisaoDoFilho(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (recursoPai == null)
            return;

        recursoPai.ReceberColisaoDoFilho(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (recursoPai == null)
            return;

        recursoPai.ReceberTriggerDoFilho(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (recursoPai == null)
            return;

        recursoPai.ReceberTriggerDoFilho(other);
    }
}