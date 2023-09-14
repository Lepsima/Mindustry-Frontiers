using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using static MessageHandler;

public class MessageDisplayerUI : MonoBehaviour {

    [SerializeField] TMP_Text senderUI;
    [SerializeField] TMP_Text messageUI;
    [SerializeField] AudioBarsUI audioBars;

    Message message;

    public void Show(Message message) {
        // Set values
        this.message = message;
        senderUI.text = message.sender;
        messageUI.text = message.message;

        // Set active
        gameObject.SetActive(true);
        audioBars.Play(message.displayTime);

        // Call hide when display time ends
        Invoke(nameof(Hide), message.displayTime);
    }

    public void Hide() {
        // Cancel invokes
        CancelInvoke(nameof(Hide));

        // Deactivate
        gameObject.SetActive(false);

        // Reset values
        senderUI.text = messageUI.text = "";
        message = Message.Empty;
    }

    public int Priority() {
        return message.priority;
    }

    public bool Displaying() {
        return gameObject.activeSelf;
    }
}
