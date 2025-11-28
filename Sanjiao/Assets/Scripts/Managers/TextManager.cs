using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct TextDisplay
{
    public TextAsset textFile;

    public Text uiTextBox;
    public GameObject textPanel;
    public Text textLabel;

    public int index;
    public float textSpeed;

    public bool textFinished ;
    public bool cancelTyping;
    public List<string> textList;


    public void Initialize()
    {
        textList = new List<string>();
        index = 0;
        textFinished = true;
        cancelTyping = false;

        if (textPanel != null)
            textPanel.SetActive(false);

        GetText(textFile);
    }

    void GetText(TextAsset file)
    {
        textList.Clear();
        index = 0;

        if (file != null)
        {
            var lineData = file.text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lineData)
            {
                textList.Add(line);
            }
        }
    }

    public bool AdvanceText()
    {
        if (textFinished && index >= textList.Count)
        {
            // �ı���ʾ���
            if (textPanel != null)
                textPanel.SetActive(false);

            if (uiTextBox != null)
                uiTextBox.text = "";
            if (textLabel != null)
                textLabel.text = "";

            return true; 
        }
        else if (textFinished && index < textList.Count)
        {
            return false; //
        }
        else
        {
            cancelTyping = true;
            return false; //
        }
    }

    IEnumerator SetTextUI()//
    {
        textFinished = false;
        if (textLabel != null)
            textLabel.text = "";

        int letter = 0;
        while (!cancelTyping && letter < textList[index].Length)
        {
            if (textLabel != null)
                textLabel.text += textList[index][letter];
            letter++;
            yield return new WaitForSeconds(textSpeed);
        }

        if (textLabel != null)
            textLabel.text = textList[index];

        cancelTyping = false;
        textFinished = true;
        index++;
    }

    public void StartDisplay()//
    {
        if (textPanel != null)
            textPanel.SetActive(true);

        index = 0;

    }

    public void EndDisplay()//
    {
        if (textPanel != null)
            textPanel.SetActive(false);

        if (uiTextBox != null)
            uiTextBox.text = "";
        if (textLabel != null)
            textLabel.text = "";
    }


}

public class TextManager : MonoBehaviour
{

    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    

    
}
