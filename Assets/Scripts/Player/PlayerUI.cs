using Frontiers.Content;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour {
    public static PlayerUI Instance;

    [SerializeField] Image selectedBlockImage;
    [SerializeField] GameObject loadingScreen;

    private void Awake() {
        Instance = this;
    }

    public void EnableLoadingScreen(bool state) {
        loadingScreen.SetActive(state);
        PostProcessingController.Instance.SetGrainActive(state);
    }

    public void SetSelectedContent(Content content) {
        if (content != null) selectedBlockImage.sprite = content.spriteFull;
        else selectedBlockImage.sprite = null;
    }
}