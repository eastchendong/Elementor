using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;



public class dialogue : MonoBehaviour
{
    public TextAsset dialogDataFile;
    public TMP_Text nameText;
    public TMP_Text dialogText;
    public int DialogIndex;
    public string[] DialogRows;
    public GameObject optionButton;
    public Transform buttonGroup;
    private bool btnfull = true;
    private bool optionsGenerated = false;
    public AudioSource audioSource; // 在Inspector中指定
    public AudioClip[] dialogAudioClips; // 在Inspector中指定，确保与对话行索引对应
    public Animator FaceAnimation;



    void Start()
    {
   
            ReadText(dialogDataFile);
            ShowDiaLogRow();

    }

    

    public void UpdateText(string _name, string _text, int audioClipIndex = -1, int animatorIntValue = -1)
    {
        nameText.text = _name;
        dialogText.text = _text;
        FaceAnimation.SetInteger("DialogueNumber", animatorIntValue);
        if (audioClipIndex >= 0 && audioClipIndex < dialogAudioClips.Length)
        {
            AudioClip clip = dialogAudioClips[audioClipIndex];
            audioSource.clip = clip;
            audioSource.Play();
            StartCoroutine(WaitForAudioClip(audioSource));
        }
    }

    public void ReadText(TextAsset _textAsset)
    {
        DialogRows = _textAsset.text.Split('\n');

    }
    public void ShowDiaLogRow()
    {
        for (int i = 0; i < DialogRows.Length; i++)
        {
            string[] cell = DialogRows[i].Split(',');
            if (cell[0] == "#" && int.Parse(cell[1]) == DialogIndex)
            {
                int audioIndex = int.TryParse(cell[5], out var index) ? index : -1;
                int animatorIntValue = int.TryParse(cell[5], out var animValue) ? animValue : -1;
                UpdateText(cell[2], cell[3], audioIndex, animatorIntValue);
                DialogIndex = int.Parse(cell[4]);
                optionsGenerated = false; // 重置选项生成标志
                break;
            }
            else if (cell[0] == "!" && int.Parse(cell[1]) == DialogIndex && !optionsGenerated)
            {

                GenerateOption(i);
                optionsGenerated = true; // 设置标志以防止再次生成
            }
            else if (cell[0] == "NEXT" && int.Parse(cell[1]) == DialogIndex)
            {

                SceneManager.LoadScene(1);

            }
            else if (cell[0] == "END" && int.Parse(cell[1]) == DialogIndex)
            {
                  Application.Quit();
            }
        }
    }


    public void GenerateOption(int _index)
    {
        // 使用正确的变量i来遍历对话行
        for (int i = _index; i < DialogRows.Length; i++)
        {
            string[] cells = DialogRows[i].Split(','); // 注意这里使用i，不是_index
            if (cells[0] == "!")
            {

                // 实例化选项按钮
                GameObject button = Instantiate(optionButton, buttonGroup);

                // 假设按钮上有一个TextMeshProUGUI组件来显示文本
                TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
                buttonText.text = cells[3];
                button.GetComponent<Button>().onClick.AddListener
                    (
                    delegate
                    {
                        OnOptionClick(int.Parse(cells[4]));

                    }
                    );
            }
            else if (cells[0] != "!" && i > _index)
            {
                // 如果遇到的行不是以"!"开头，并且已经处理过至少一个选项，中断循环
                break;
            }
        }
    }
    public void OnOptionClick(int _id)
    {
        DialogIndex = _id;
        ShowDiaLogRow();
        // 输出日志，确认此方法被调用
        Debug.Log("OnOptionClick called with _id: " + _id);
        foreach (Transform child in buttonGroup)
        {
            Destroy(child.gameObject);
        }
    }
    IEnumerator WaitForAudioClip(AudioSource source)
    {
        yield return new WaitWhile(() => source.isPlaying);
        ShowDiaLogRow();
    }
}