using UnityEngine;

[CreateAssetMenu(fileName = "ModeScriptable", menuName = "Scriptable Object/ModeScriptable", order = int.MaxValue)]
public class ModeScriptable : ScriptableObject
{
    public int boardSize;
    public int winCount;
    public Sprite spriteBoard;
}
