using UnityEngine;

[DisallowMultipleComponent]
public class AviaoGaragem : MonoBehaviour
{
    private enum Estado { Movendo, Rotacionando, Concluido }

    public enum DirecaoRotacao { Horario, AntiHorario }

    [Header("Ponto de Decolagem")]
    [Tooltip("Tag usada nos GameObjects que marcam os pontos de decolagem de cada base")]
    [SerializeField] private string tagPontoDecolagem = "PontoDecolagem";

    [Header("Movimento")]
    [SerializeField] private float velocidadeMovimento = 8f;

    [Header("Rotação")]
    [SerializeField] private float velocidadeRotacao = 45f;
    [SerializeField] private DirecaoRotacao direcaoRotacao = DirecaoRotacao.Horario;

    [Header("Somente leitura")]
    [SerializeField] private Transform pontoEncontrado; // referência encontrada automaticamente

    // ── Privados ──────────────────────────────────────────────────────────

    private Estado    estado = Estado.Movendo;
    private Vector3   destino;
    private Quaternion rotacaoCongelada;
    private float     yAlvo;

    private const float DISTANCIA_CHEGADA = 0.15f;
    private const float GRAUS_ROTACAO     = 90f;
    private const float TOLERANCIA_ROT    = 0.3f;

    // ── Unity ─────────────────────────────────────────────────────────────

    private void Start()
    {
        pontoEncontrado = BuscarPontoMaisProximo();

        if (pontoEncontrado == null)
        {
            enabled = false;
            return;
        }

        destino          = new Vector3(pontoEncontrado.position.x,
                                       transform.position.y,
                                       pontoEncontrado.position.z);
        rotacaoCongelada = transform.rotation;
    }

    private void Update()
    {
        switch (estado)
        {
            case Estado.Movendo:      Mover();      break;
            case Estado.Rotacionando: Rotacionar(); break;
        }
    }

    // ── Busca automática ──────────────────────────────────────────────────

    private Transform BuscarPontoMaisProximo()
    {
        GameObject[] candidatos = GameObject.FindGameObjectsWithTag(tagPontoDecolagem);

        if (candidatos == null || candidatos.Length == 0)
            return null;

        Transform melhor       = null;
        float     menorDistancia = float.MaxValue;

        foreach (GameObject obj in candidatos)
        {
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < menorDistancia)
            {
                menorDistancia = dist;
                melhor         = obj.transform;
            }
        }

        return melhor;
    }

    // ── Movimento (rotação travada) ────────────────────────────────────────

    private void Mover()
    {
        transform.rotation = rotacaoCongelada;

        if (Vector3.Distance(transform.position, destino) <= DISTANCIA_CHEGADA)
        {
            transform.position = destino;
            IniciarRotacao();
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position, destino, velocidadeMovimento * Time.deltaTime);
    }

    // ── Rotação ───────────────────────────────────────────────────────────

    private void IniciarRotacao()
    {
        float sinal = direcaoRotacao == DirecaoRotacao.Horario ? 1f : -1f;
        yAlvo  = transform.eulerAngles.y + sinal * GRAUS_ROTACAO;
        estado = Estado.Rotacionando;
    }

    private void Rotacionar()
    {
        float restante = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, yAlvo));

        if (restante <= TOLERANCIA_ROT)
        {
            Vector3 e = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(e.x, yAlvo, e.z);
            estado  = Estado.Concluido;
            enabled = false;
            return;
        }

        float sinal = direcaoRotacao == DirecaoRotacao.Horario ? 1f : -1f;
        float passo = Mathf.Min(restante, velocidadeRotacao * Time.deltaTime);
        transform.Rotate(Vector3.up, sinal * passo, Space.World);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Em runtime mostra o ponto encontrado automaticamente
        Transform alvo = Application.isPlaying
            ? pontoEncontrado
            : BuscarPontoMaisProximoEditor();

        if (alvo == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, alvo.position);
        Gizmos.DrawWireSphere(alvo.position, 0.5f);

        float sinal  = direcaoRotacao == DirecaoRotacao.Horario ? 1f : -1f;
        float yFinal = alvo.eulerAngles.y + sinal * GRAUS_ROTACAO;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(alvo.position,
                       Quaternion.Euler(0, yFinal, 0) * Vector3.forward * 3f);
    }

    // Versão do editor (fora do runtime) para visualizar antes de rodar
    private Transform BuscarPontoMaisProximoEditor()
    {
        if (string.IsNullOrEmpty(tagPontoDecolagem)) return null;

        try
        {
            GameObject[] candidatos = GameObject.FindGameObjectsWithTag(tagPontoDecolagem);
            Transform melhor        = null;
            float     menorDistancia = float.MaxValue;

            foreach (GameObject obj in candidatos)
            {
                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist < menorDistancia) { menorDistancia = dist; melhor = obj.transform; }
            }

            return melhor;
        }
        catch { return null; }
    }
}