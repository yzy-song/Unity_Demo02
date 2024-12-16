using UnityEngine;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public CanvasGroup loadingPanel; // 加载指示器的父对象
    public Text loadingText; // 动态文本
    public float fadeDuration = 0.3f; // 淡入淡出持续时间

    private bool isShowing = false;

    private void Awake()
    {
        HideLoadingInstant(); // 确保初始状态是隐藏
    }

    public void ShowLoading(string message)
    {
        if (isShowing) return;

        isShowing = true;
        loadingText.text = message;
        loadingPanel.interactable = true;
        loadingPanel.blocksRaycasts = true; // 阻止其他交互
        StartCoroutine(FadeCanvasGroup(loadingPanel, 0, 1, fadeDuration));
    }

    public void HideLoading()
    {
        if (!isShowing) return;

        isShowing = false;
        loadingPanel.interactable = false;
        loadingPanel.blocksRaycasts = false; // 恢复其他交互
        StartCoroutine(FadeCanvasGroup(loadingPanel, 1, 0, fadeDuration));
    }

    private void HideLoadingInstant()
    {
        isShowing = false;
        loadingPanel.alpha = 0;
        loadingPanel.interactable = false;
        loadingPanel.blocksRaycasts = false;
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
