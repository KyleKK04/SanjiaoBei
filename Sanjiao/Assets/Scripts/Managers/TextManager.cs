using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct TextDisplay
{
    public TextAsset textFile;//文本文件引用

    public Text uiTextBox;//UI文本框引用
    public GameObject textPanel;//显示/隐藏文本面板
    public Text textLabel;

    public int index;
    public float textSpeed;

    public bool textFinished ;
    public bool cancelTyping;
    public List<string> textList;


    public void Initialize()//初始化
    {
        textList = new List<string>();
        index = 0;
        textFinished = true;
        cancelTyping = false;

        if (textPanel != null)
            textPanel.SetActive(false);

        GetText(textFile);
    }

    void GetText(TextAsset file)//获取文本
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

    public bool AdvanceText()//推进文本
    {
        if (textFinished && index >= textList.Count)
        {
            // 文本显示完毕
            if (textPanel != null)
                textPanel.SetActive(false);

            if (uiTextBox != null)
                uiTextBox.text = "";
            if (textLabel != null)
                textLabel.text = "";

            return true; // 文本结束
        }
        else if (textFinished && index < textList.Count)
        {
            return false; // 需要继续显示下一句
        }
        else
        {
            cancelTyping = true;
            return false; // 正在显示中
        }
    }

    IEnumerator SetTextUI()//设置文本UI
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

    public void StartDisplay()//开始显示
    {
        if (textPanel != null)
            textPanel.SetActive(true);

        index = 0;

    }

    public void EndDisplay()//结束显示
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
