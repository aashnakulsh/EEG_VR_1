using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogPagerSingle : MonoBehaviour
{
    [Serializable]
    public class Page
    {
        [TextArea] public string title;
        [TextArea] public string body;
        [TextArea] public string subtitle; // optional
    }

    [Header("UI References (TMP)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text subtitleText; // can be null

    [Header("Optional: Button GameObjects (for enable/disable visuals)")]
    [SerializeField] private GameObject nextButtonObject;
    [SerializeField] private GameObject backButtonObject;

    [Header("Pages")]
    [SerializeField] private List<Page> pages = new List<Page>();
    [SerializeField] private int startIndex = 0;

    private int index;

    private void Awake()
    {
        index = Mathf.Clamp(startIndex, 0, Math.Max(0, pages.Count - 1));
        Refresh();
    }

    public void Next()
    {
        if (pages == null || pages.Count == 0) return;
        if (index >= pages.Count - 1) return;
        index++;
        Refresh();
    }

    public void Back()
    {
        if (pages == null || pages.Count == 0) return;
        if (index <= 0) return;
        index--;
        Refresh();
    }

    private void Refresh()
    {
        if (pages == null || pages.Count == 0)
        {
            if (titleText) titleText.text = "";
            if (bodyText) bodyText.text = "";
            if (subtitleText) subtitleText.text = "";
            SetButtonState(false, false);
            return;
        }

        var p = pages[index];

        if (titleText) titleText.text = p.title ?? "";
        if (bodyText) bodyText.text = p.body ?? "";
        if (subtitleText) subtitleText.text = p.subtitle ?? "";

        SetButtonState(index < pages.Count - 1, index > 0);
    }

    private void SetButtonState(bool nextEnabled, bool backEnabled)
    {
        if (nextButtonObject) nextButtonObject.SetActive(nextEnabled);
        if (backButtonObject) backButtonObject.SetActive(backEnabled);
    }
}
