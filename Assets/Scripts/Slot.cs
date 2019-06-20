using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class Slot : MonoBehaviour, IDropHandler
{
    public int row, col;
    public GameObject item
    {
        get
        {
            if (transform.childCount > 0)
            {
                return transform.GetChild(0).gameObject;
            }

            return null;
        }
    }

    #region IdropHandler implementation
    public void OnDrop(PointerEventData eventData)
    {
        
        if (!item)
        {
            MyControll.draggedObject.transform.SetParent(transform);
            ExecuteEvents.ExecuteHierarchy<IHasChanged>(gameObject, null, (x, y) => x.hasChanged());
        }
        else if (item)
        {
            item.GetComponent<MyControll>().setToDefault();
            MyControll.draggedObject.transform.SetParent(transform);
            ExecuteEvents.ExecuteHierarchy<IHasChanged>(gameObject, null, (x, y) => x.hasChanged());
        }
        WordsGame.Instance.grid[row][col] = item.GetComponent<MyControll>().huruf;
        Debug.Log(WordsGame.Instance.grid[row]);
    }
    #endregion
}