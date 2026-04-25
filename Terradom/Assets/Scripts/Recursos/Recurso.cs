using UnityEngine;

[DisallowMultipleComponent]
public class Recurso : MonoBehaviour
{
    [Header("Quantidade do Recurso")]
    [SerializeField] private int valorMaxima = 3;
    [SerializeField] private int valorAtual = 3;

    [Header("Tags que causam dano")]
    [SerializeField] private string[] tagsQueDaoDano = { "Picareta" };

    private void Awake()
    {
        valorAtual = valorMaxima;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
            return;

        if (ColisaoTemTagQueDaDano(collision))
        {
            AplicarDano();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        if (ObjetoOuPaisTemAlgumaTagDeDano(other.transform))
        {
            AplicarDano();
        }
    }

    public void ProcessarColisao(GameObject outroObjeto)
    {
        if (outroObjeto == null)
            return;

        if (ObjetoOuPaisTemAlgumaTagDeDano(outroObjeto.transform))
        {
            AplicarDano();
        }
    }

    private void AplicarDano()
    {
        valorAtual -= 1;

        if (valorAtual <= 0)
        {
            valorAtual = 0;
            Destroy(gameObject);
        }
    }

    private bool ColisaoTemTagQueDaDano(Collision collision)
    {
        if (collision.collider != null && ObjetoOuPaisTemAlgumaTagDeDano(collision.collider.transform))
            return true;

        if (collision.transform != null && ObjetoOuPaisTemAlgumaTagDeDano(collision.transform))
            return true;

        int totalContatos = collision.contactCount;
        for (int i = 0; i < totalContatos; i++)
        {
            ContactPoint contato = collision.GetContact(i);

            if (contato.otherCollider != null && ObjetoOuPaisTemAlgumaTagDeDano(contato.otherCollider.transform))
                return true;
        }

        return false;
    }

    private bool ObjetoOuPaisTemAlgumaTagDeDano(Transform alvo)
    {
        if (alvo == null || tagsQueDaoDano == null || tagsQueDaoDano.Length == 0)
            return false;

        Transform atual = alvo;

        while (atual != null)
        {
            if (TemTagDeDano(atual))
                return true;

            atual = atual.parent;
        }

        return false;
    }

    private bool TemTagDeDano(Transform alvo)
    {
        if (alvo == null || tagsQueDaoDano == null)
            return false;

        for (int i = 0; i < tagsQueDaoDano.Length; i++)
        {
            string tagAtual = tagsQueDaoDano[i];

            if (string.IsNullOrWhiteSpace(tagAtual))
                continue;

            if (alvo.CompareTag(tagAtual))
                return true;
        }

        return false;
    }
}