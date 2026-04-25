using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Tela : MonoBehaviour
{
    [Header("Movimento da câmera")]
    public float velocidadeMovimento = 25f;

    [Header("Zoom / Altura da câmera")]
    public float velocidadeZoom = 40f;
    public float limiteAproximacaoY = 80f;
    public float limiteAfastamentoY = 300f;

    [Header("Limites do mapa")]
    public bool usarLimites = true;
    public float limiteMinX = -60f;
    public float limiteMaxX = 60f;
    public float limiteMinZ = -60f;
    public float limiteMaxZ = 60f;

    [Header("Canvas")]
    [SerializeField] private Canvas canvasUI;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (canvasUI != null && canvasUI.renderMode == RenderMode.ScreenSpaceCamera)
            canvasUI.worldCamera = cam;
    }

    private void Update()
    {
        MoverCameraComTeclado();
        AjustarAlturaComScroll();
    }

    private void MoverCameraComTeclado()
    {
        if (Keyboard.current == null)
            return;

        Vector3 direcao = Vector3.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            direcao.z += 1f;

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            direcao.z -= 1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            direcao.x += 1f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            direcao.x -= 1f;

        if (direcao.sqrMagnitude > 1f)
            direcao.Normalize();

        Vector3 novaPos = transform.position + direcao * velocidadeMovimento * Time.deltaTime;

        if (usarLimites)
        {
            novaPos.x = Mathf.Clamp(novaPos.x, limiteMinX, limiteMaxX);
            novaPos.z = Mathf.Clamp(novaPos.z, limiteMinZ, limiteMaxZ);
        }

        transform.position = novaPos;
    }

    private void AjustarAlturaComScroll()
    {
        if (Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        Vector3 novaPos = transform.position;

        novaPos.y -= scroll * velocidadeZoom * Time.deltaTime;
        novaPos.y = Mathf.Clamp(novaPos.y, limiteAproximacaoY, limiteAfastamentoY);

        transform.position = novaPos;
    }
}