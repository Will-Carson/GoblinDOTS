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
        DialogueManager = Object.FindObjectOfType<DialogueManager>();
    }

    protected override void OnMessage(DialogueMessage message)
    {
        DialogueManager.ProcessDialogueRequest(message);
    }

    protected override void OnUpdate()
    {
        
    }
}
