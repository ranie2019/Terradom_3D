using UnityEngine;
using UnityEngine.UI;

public class BarraVidaUI : MonoBehaviour
{
    [SerializeField] private Image barraVida;

    private float vidaMaxima;
    private float vidaAtual;

    // FIX: Camera.main era chamado 2x em LateUpdate a cada frame para CADA barra de vida.
    // Camera.main usa FindFirstObjectByType internamente — com 20+ unidades isso vira
    // 40+ buscas por frame. Agora é cacheado uma vez em Awake e atualizado só se a câmera mudar.
    private Camera _cameraCache;

    private void Awake()
    {
        // Desativa raycast em todas as imagens para não bloquear cliques na base
        foreach (Image img in GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;

        GraphicRaycaster gr = GetComponent<GraphicRaycaster>();
        if (gr != null) gr.enabled = false;

        CachearCamera();
    }

    private void CachearCamera()
    {
        _cameraCache = Camera.main;
    }

    public void Configurar(float vida)
    {
        vidaMaxima = vida;
        vidaAtual  = vida;
        AtualizarBarra();
    }

    public void AtualizarVida(float novaVida)
    {
        vidaAtual = novaVida;
        AtualizarBarra();
    }

    private void AtualizarBarra()
    {
        if (barraVida != null && vidaMaxima > 0f)
            barraVida.fillAmount = vidaAtual / vidaMaxima;
    }

    private void LateUpdate()
    {
        // Revalida o cache se a câmera foi destruída ou trocada (ex: entre cenas)
        if (_cameraCache == null)
            CachearCamera();

        if (_cameraCache == null) return;

        transform.forward = _cameraCache.transform.forward;
    }
}