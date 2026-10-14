using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExChoosingMgr : MonoBehaviour
{
    public Text ExNameShower;
    private RectTransform NowExButton;
    private int NowExButtonIndex;
    public RectTransform[] ExButtons;


    // Start is called before the first frame update
    void Start()
    {

        NowExButton = ExButtons[0];
        NowExButtonIndex = 0;

        ExNameShower.text = NowExButton.name;
        ShowButtons();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ShowButtons()
    {
        int lenth = ExButtons.Length;
        NowExButton.anchoredPosition = new Vector2(0, 50);
        NowExButton.localScale *= 1.5f;
        for (int i = NowExButtonIndex + 1; i < lenth; i++)
        {
            ExButtons[i].anchoredPosition = new Vector2((i - NowExButtonIndex) * 300, 50);
        }
        for (int i = NowExButtonIndex - 1; i >= 0; i--)
        {
            ExButtons[i].anchoredPosition = new Vector2((i - NowExButtonIndex) * 300, 50);
        }
    }

    public void NextEx()
    {
        if (NowExButtonIndex < ExButtons.Length - 1)
        {
            NowExButtonIndex++;
            NowExButton.localScale /= 1.5f;
            NowExButton = ExButtons[NowExButtonIndex];
            ExNameShower.text = NowExButton.name;
            ShowButtons();
        }
    }

    public void LastEx()
    {
        if (NowExButtonIndex > 0)
        {
            NowExButtonIndex--;
            NowExButton.localScale /= 1.5f;
            NowExButton = ExButtons[NowExButtonIndex];
            ExNameShower.text = NowExButton.name;
            ShowButtons();
        }
    }

    public void LoadEx()
    {
        string sceneName = NowExButton.name;
        if (string.IsNullOrEmpty(sceneName) || sceneName == "WaitingForUpdate")
        {
            Debug.LogWarning("[ExChoosingMgr] sceneName 未配置");
            return;
        }
        Time.timeScale = 1f; // 防止从暂停态切场景
        SceneMgr.Instance.LoadSceneWithLoading(sceneName, "LoadingScene");
    }

}
