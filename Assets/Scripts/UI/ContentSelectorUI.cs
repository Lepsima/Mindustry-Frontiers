using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using System.Collections.Generic;
using System.Collections;

public class ContentSelectorUI : MonoBehaviour {
    public Transform contentBar;
    public Transform typesBar;

    private readonly List<ContentListItem>[] contentButtonLists = new List<ContentListItem>[3] { new List<ContentListItem>(), new List<ContentListItem>(), new List<ContentListItem>() };
    private GameObject contentButtonPrefab;

    private void Start() {
        contentButtonPrefab = AssetLoader.GetPrefab("ContentPrefabUI");

        InstantiateContentButtons<BlockType>(contentButtonLists[0]);
        InstantiateContentButtons<UnitType>(contentButtonLists[1]);
        InstantiateContentButtons<Element>(contentButtonLists[2]);

        OnChangeContentType(0);
    }

    private void InstantiateContentButtons<T>(List<ContentListItem> addToList) where T : Content{
        T[] contentArray = ContentLoader.GetContentByType<T>();
        foreach (T content in contentArray) if (!content.hidden) addToList.Add(AddContent(contentBar, content));
    }

    private ContentListItem AddContent(Transform parent, Content content) {
        ContentListItem contentListItem = Instantiate(contentButtonPrefab, parent).GetComponent<ContentListItem>();
        contentListItem.SetUp(content);
        contentListItem.OnUserClick += OnItemClicked;
        return contentListItem;
    }

    public void OnItemClicked(object sender, ContentListItem.ContentArgs e) {
        PlayerContentSelector.SetSelectedContent(e.content);
    }

    public void OnChangeContentType(int index) {
        for (int i = 0; i < contentButtonLists.Length; i++) {
            List<ContentListItem> contentButtonList = contentButtonLists[i];
            foreach(ContentListItem contentListItem in contentButtonList) contentListItem.transform.gameObject.SetActive(i == index);
        }
    }

    public void AddItems(int amount) {
        PlayerManager.Instance.AddItems(amount);
    }
}