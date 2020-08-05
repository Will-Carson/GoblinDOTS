using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DOTSNET;

[ClientWorld]
public class ProcessDialogueRequestMessage : NetworkClientMessageSystem<DialogueMessage>
{
    private DialogueManager DialogueManager;

    protected override void OnCreate()
    {
        base.OnCreate();
        DialogueManager = Object.FindObjectOfType<DialogueManager>();
    }

    protected override void OnMessage(DialogueMessage message)
    {
        Debug.Log(message.dialogueId);
        DialogueManager.ProcessDialogueRequest(message);
    }

    protected override void OnUpdate() { }
}
