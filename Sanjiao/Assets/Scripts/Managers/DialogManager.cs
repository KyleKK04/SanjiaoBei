using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Visual;
using System.Threading.Tasks;
using Game.Utilities;

namespace Game.Core
{
    public class DialogueManager : Singleton<DialogueManager>
    {
        private const string PANEL_NAME = "Dialog";

        /// <summary>
        /// 发起对话（支持单句或多句）
        /// </summary>
        /// <param name="lines">对话列表</param>
        public async void ShowDialogue(List<DialogueLine> lines)
        {
            if (lines == null || lines.Count == 0) return;

            // 1. 暂停游戏
            Time.timeScale = 0f;

            // 2. 打开面板 (等待面板完全打开)
            await UIManager.Instance.OpenPanelAsync(PANEL_NAME);

            // 3. 获取面板上的控制器脚本并开始
            GameObject panelObj = UIManager.Instance.GetOrInstantiatePanel(PANEL_NAME);
            if (panelObj != null)
            {
                DialoguePanel panelScript = panelObj.GetComponent<DialoguePanel>();
                if (panelScript != null)
                {
                    panelScript.StartDialogue(lines);
                }
                else
                {
                    Debug.LogError("DialogPanel预制体上缺少 DialoguePanel 脚本！");
                    EndDialogue(); // 异常恢复
                }
            }
        }

        /// <summary>
        /// 发起单句对话的重载
        /// </summary>
        public void ShowDialogue(DialogueLine singleLine)
        {
            ShowDialogue(new List<DialogueLine> { singleLine });
        }

        /// <summary>
        /// 结束对话 (由 Panel 调用)
        /// </summary>
        public async void EndDialogue()
        {
            // 1. 关闭面板
            await UIManager.Instance.ClosePanelAsync(PANEL_NAME);

            // 2. 恢复游戏时间
            Time.timeScale = 1f;
            
            Debug.Log("Dialogue Finished.");
        }
    }
}