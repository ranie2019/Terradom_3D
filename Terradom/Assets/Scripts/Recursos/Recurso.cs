using UnityEngine;

[DisallowMultipleComponent]
public class Recurso : MonoBehaviour
{
    [Header("Quantidade")]
    [SerializeField] private int valorMaximo = 10;
    [SerializeField] private int valorAtual = 10;

    [Header("Coleta")]
    [SerializeField] private int valorPorColeta = 1;
    [SerializeField] private float intervaloEntreColetas = 0.25f;

    [Header("Ferramentas")]
    [SerializeField] private string tagPicareta = "Picareta";
    [SerializeField] private string tagMachado = "Machado";

    [Header("Jogadores")]
    [SerializeField] private string[] tagsDosDonos = { "Azul", "Vermelho" };

    private float proximaColeta = 0f;

    private void Awake()
    {
        valorAtual = valorMaximo;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        TentarColetar(other.transform);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        TentarColetar(collision.collider.transform);
    }

    private void TentarColetar(Transform objetoQueTocou)
    {
        if (Time.time < proximaColeta)
            return;

        Transform ferramenta = EncontrarFerramenta(objetoQueTocou);

        if (ferramenta == null)
            return;

        string tipoRecurso = ObterTipoRecursoPelaTagDoObjeto();

        if (string.IsNullOrWhiteSpace(tipoRecurso))
            return;

        if (!FerramentaPodeColetar(ferramenta, tipoRecurso))
            return;

        string tagDono = EncontrarTagDono(ferramenta);

        if (string.IsNullOrWhiteSpace(tagDono))
            return;

        proximaColeta = Time.time + intervaloEntreColetas;

        valorAtual -= valorPorColeta;

        if (GameControllerRecursos.Instance != null)
        {
            GameControllerRecursos.Instance.AdicionarRecurso(
                tagDono,
                tipoRecurso,
                valorPorColeta
            );
        }

        if (valorAtual <= 0)
        {
            valorAtual = 0;
            Destroy(gameObject);
        }
    }

    private string ObterTipoRecursoPelaTagDoObjeto()
    {
        if (CompareTag("Pedra"))
            return "Pedra";

        if (CompareTag("Arvore"))
            return "Arvore";

        if (CompareTag("Metal"))
            return "Metal";

        return "";
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
        if (ferramenta.CompareTag(tagPicareta) && tipoRecurso == "Pedra")
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
}