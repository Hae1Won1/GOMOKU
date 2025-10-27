using UnityEngine;
using UnityEngine.UI;

public class ToggleGropDef : MonoBehaviour
{
    public Toggle toggle;

    private void OnEnable()
    {
        toggle.isOn = true;
    }
}
