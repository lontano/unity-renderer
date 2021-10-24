using System;
using DCL.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetaverseController : MonoBehaviour
{
    [SerializeField] private Button open;
    [SerializeField] private Button close;
    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI clock;

    [Header("Jump Ins")]
    [SerializeField] private Button smash;
    [SerializeField] private Button acoustic;
    [SerializeField] private Button techno;
    [SerializeField] private Button world;
    [SerializeField] private Button evolution;

    private static readonly Vector2Int smashCoords = new Vector2Int(-67, 84);
    private static readonly Vector2Int acousticCoords = new Vector2Int(-66, 81);
    private static readonly Vector2Int technoCoords = new Vector2Int(-62, 81);
    private static readonly Vector2Int worldCoords = new Vector2Int(-61, 85);
    private static readonly Vector2Int evolutionCoords = new Vector2Int(-64, 92);

    void Start()
    {
        container.SetActive(false);
        open.onClick.AddListener(OnOpen);
        close.onClick.AddListener(OnClose);

        smash.onClick.AddListener(() => OnJumpIn(smashCoords));
        acoustic.onClick.AddListener(() => OnJumpIn(acousticCoords));
        techno.onClick.AddListener(() => OnJumpIn(technoCoords));
        world.onClick.AddListener(() => OnJumpIn(worldCoords));
        evolution.onClick.AddListener(() => OnJumpIn(evolutionCoords));
    }

    private void Update() { clock.text = DateTime.UtcNow.ToString("HH:mm:ss"); }

    private void OnJumpIn(Vector2Int coords)
    {
        OnClose();
        WebInterface.SendChatMessage(new ChatMessage
        {
            messageType = ChatMessage.Type.NONE,
            recipient = string.Empty,
            body = $"/goto {coords.x},{coords.y}",
        });
    }

    private void OnOpen() { container.SetActive(true); }

    private void OnClose() { container.SetActive(false); }

}