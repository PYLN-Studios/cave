using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "New Map Set", menuName = "Lobby/Map Set")]
public class MapSet : ScriptableObject
{
    [Scene]
    [Tooltip("Add game scenes here, played in order")]
    public string[] maps;
}
