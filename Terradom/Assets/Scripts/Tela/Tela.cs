using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
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
        ConfigurarCanvas();
        CorrigirLimitesInvertidos();
    }

    private void Update()
    {
        MoverCameraComTeclado();
        AjustarAlturaComScroll();
    }

    private void ConfigurarCanvas()
    {
        if (canvasUI == null || cam == null)
            return;

        if (canvasUI.renderMode == RenderMode.ScreenSpaceCamera)
            canvasUI.worldCamera = cam;
    }

    private void CorrigirLimitesInvertidos()
    {
        if (limiteMinX > limiteMaxX)
            Trocar(ref limiteMinX, ref limiteMaxX);

        if (limiteMinZ > limiteMaxZ)
            Trocar(ref limiteMinZ, ref limiteMaxZ);

        if (limiteAproximacaoY > limiteAfastamentoY)
            Trocar(ref limiteAproximacaoY, ref limiteAfastamentoY);
    }

    private void MoverCameraComTeclado()
    {
        if (Keyboard.current == null)
            return;

        Vector3 direcao = ObterDirecaoTeclado();

        if (direcao.sqrMagnitude < 0.0001f)
            return;

        direcao.Normalize();

        Vector3 novaPos = transform.position + direcao * velocidadeMovimento * Time.deltaTime;
        novaPos = AplicarLimitesXZ(novaPos);

        transform.position = novaPos;
    }

    private Vector3 ObterDirecaoTeclado()
    {
        Vector3 direcao = Vector3.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            direcao.z += 1f;

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            direcao.z -= 1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            direcao.x += 1f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            direcao.x -= 1f;

        return direcao;
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

    private Vector3 AplicarLimitesXZ(Vector3 pos)
    {
        if (!usarLimites)
            return pos;

        pos.x = Mathf.Clamp(pos.x, limiteMinX, limiteMaxX);
        pos.z = Mathf.Clamp(pos.z, limiteMinZ, limiteMaxZ);

        return pos;
    }

    private void Trocar(ref float a, ref float b)
    {
        float temp = a;
        a = b;
        b = temp;
    }
}