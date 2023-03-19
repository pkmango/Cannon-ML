using UnityEngine;
using UnityEngine.UI;

public class DemoUI : MonoBehaviour
{
    public GunAgent player;
    public Text pointsText;
    public Text hpText;

    void Update()
    {
        pointsText.text = player.currentPoints.ToString();
        hpText.text = player.currentHp.ToString();
    }
}
