using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public Dictionary<int, DialogueBox> actors = new Dictionary<int, DialogueBox>();
    public Dictionary<int, DialogueContent> content = new Dictionary<int, DialogueContent>();

    private void Start()
    {
        // TODO TEST
        actors.Add(2, FindObjectOfType<DialogueBox>());
    }

    public void AddActor()
    {

    }

    public void RemoveActor()
    {

    }

    public void ProcessDialogueRequest(DialogueMessage request)
    {
        actors[request.actorId].PlayDialogue(content[request.dialogueId]);
    }

    public void SendDialogueRequest()
    {

    }

    public void SendPlayLineRequest()
    {

    }
}

public struct DialogueContent
{
    public string line;
}