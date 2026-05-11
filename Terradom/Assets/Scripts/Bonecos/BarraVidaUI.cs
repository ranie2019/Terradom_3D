using UnityEngine;
using UnityEngine.UI;

public class BarraVidaUI : MonoBehaviour
{
    [SerializeField] private Image barraVida;

    private float vidaMaxima;
    private float vidaAtual;

    private void Awake()
    {
        // Desativa raycast em todas as imagens para não bloquear cliques na base
        foreach (Image img in GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;

        GraphicRaycaster gr = GetComponent<GraphicRaycaster>();
        if (gr != null) gr.enabled = false;
    }

    public void Configurar(float vida)
    {
        vidaMaxima = vida;
        vidaAtual = vida;
        AtualizarBarra();
    }

    public void AtualizarVida(float novaVida)
    {
        vidaAtual = novaVida;
        AtualizarBarra();
    }

    private void AtualizarBarra()
    {
        if (barraVida != null)
            barraVida.fillAmount = vidaAtual / vidaMaxima;
    }

    private void LateUpdate()
    {
        if (Camera.main == null) return;

        // Sempre olha para a câmera
        transform.forward = Camera.main.transform.forward;
    }
}
