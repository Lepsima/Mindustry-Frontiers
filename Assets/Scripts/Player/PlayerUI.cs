using Frontiers.Content;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour {
    public static PlayerUI Instance;

    [SerializeField] Image selectedBlockImage;

    private void Awake() {
        Instance = this;
    }

    public void SetSelectedContent(Content content) {
        if (content != null) selectedBlockImage.sprite = content.sprite;
        else selectedBlockImage.sprite = null;
    }
}