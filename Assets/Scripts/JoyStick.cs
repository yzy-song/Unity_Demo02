using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform joystickBackground;  // 摇杆背景
    public RectTransform joystickHandle;      // 摇杆把手
    public float joystickRadius = 100f;       // 摇杆活动半径

    private Vector2 inputVector;              // 最终的输入向量

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out position
        );

        inputVector = position / joystickRadius;
        inputVector = inputVector.magnitude > 1.0f ? inputVector.normalized : inputVector;

        // 移动摇杆把手
        joystickHandle.anchoredPosition = inputVector * joystickRadius;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData); // 同时处理按下时的拖拽
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero; // 重置摇杆
    }

    public Vector2 GetInput()
    {
        return inputVector;
    }
}
