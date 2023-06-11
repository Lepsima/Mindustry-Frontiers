using UnityEngine;
using UnityEngine.UI;
using Frontiers.Content;
using System;

public class ContentListItem : MonoBehaviour {
    public class ContentArgs { public Content content; }
    public event EventHandler<ContentArgs> OnUserClick;

    [SerializeField] Image image;
    public Content content;

    public void SetUp(Content content) {
        this.content = content;
        image.sprite = content.spriteFull;
    }

    public void OnClick() {
        OnUserClick?.Invoke(this, new ContentArgs() { content = content });
    }
}