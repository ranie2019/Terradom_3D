using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BaseVida : MonoBehaviour
{
    [Header("Vida da base")]
    [SerializeField] private int vidaMaxima = 10;
    [SerializeField] private int vidaAtual = 10;

    [Header("Dano por colisão")]
    [SerializeField] private string tagInimigo = "Inimigo";
    [SerializeField] private int danoPorColisao = 1;

    private void Start()
    {
        vidaAtual = vidaMaxima;
    }

    private void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag(tagInimigo))
        {
            ReceberDano(danoPorColisao);
        }
    }

    public void ReceberDano(int dano)
    {
        vidaAtual -= dano;

        if (vidaAtual <= 0)
        {
            vidaAtual = 0;
            DestruirBase();
        }
    }

    private void DestruirBase()
    {
        Destroy(gameObject);
    }

    public int GetVidaAtual()
    {
        return vidaAtual;
    }
}