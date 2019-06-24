using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MyControll : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    public static GameObject draggedObject;
    [SerializeField] public string huruf;
    [SerializeField] public int urutan, point;
    [SerializeField] public bool canDrag = true;
    Vector3 startPosition, defaultPosition;
    Transform startParent, defaultParent;

    void Start()
    {
        defaultPosition = transform.position;
        defaultParent = transform.parent;
    }
    public void setToDefault()
    {
        transform.position = defaultPosition;
        transform.SetParent(defaultParent);
    }
    #region IBeginDragHandler implementation
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canDrag)
        {
            draggedObject = gameObject;
            startPosition = transform.position;
            startParent = transform.parent;
            GetComponent<CanvasGroup>().blocksRaycasts = false;
            transform.SetParent(transform.root);
        }
    }
    #endregion

    #region IDragHandler implementation
    public void OnDrag(PointerEventData eventData)
    {
        if (canDrag)
            transform.position = Input.mousePosition;

    }
    #endregion

    #region IEndDragHandler implementation
    public void OnEndDrag(PointerEventData eventData)
    {
        draggedObject = null;
        if (transform.parent == startParent)
        {
            transform.position = startPosition;
            transform.SetParent(startParent);
        }
        else if(transform.parent == transform.root)
        {
            setToDefault();
            ExecuteEvents.ExecuteHierarchy<IHasChanged>(gameObject, null, (x, y) => x.hasChanged());
        }

        GetComponent<CanvasGroup>().blocksRaycasts = true;
    }
    #endregion

}
