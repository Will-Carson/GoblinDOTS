using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueBox : MonoBehaviour
{
    private TextMesh textBox;

    private void Start()
    {
        textBox = GetComponent<TextMesh>();
    }

    internal void PlayDialogue(DialogueContent dialogueContent)
    {
        textBox.text = dialogueContent.line;
    }
}
