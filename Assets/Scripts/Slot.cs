using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class Slot : MonoBehaviour, IDropHandler
{
    [SerializeField] public int row, col;
    [SerializeField] public bool isDefaultSlot = false;
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
        if (MyControll.draggedObject != null)
        {
            if (!isDefaultSlot) //Jika huruf di drop ke slot grid 15x15
            {
                if (!item)
                {
                    MyControll.draggedObject.transform.SetParent(transform);
                    ExecuteEvents.ExecuteHierarchy<IHasChanged>(gameObject, null, (x, y) => x.hasChanged());
                }
                else if (item)
                {
                    if (item.GetComponent<MyControll>().canDrag == false)
                        MyControll.draggedObject.GetComponent<MyControll>().setToDefault();
                    else
                    {
                        if (isDefaultSlot) MyControll.draggedObject.GetComponent<MyControll>().setToDefault();
                        else MyControll.draggedObject.transform.SetParent(transform);


                        item.GetComponent<MyControll>().setToDefault();
                    }
                    ExecuteEvents.ExecuteHierarchy<IHasChanged>(gameObject, null, (x, y) => x.hasChanged());
                }
                WordsGame.Instance.grid[row][col] = item.GetComponent<MyControll>();
                WordsGame.Instance.wordSet[item.GetComponent<MyControll>().urutan][0] = row;
                WordsGame.Instance.wordSet[item.GetComponent<MyControll>().urutan][1] = col;
            }
            else
            {
                MyControll.draggedObject.GetComponent<MyControll>().setToDefault();
            }
            WordsGame.Instance.playSlotAudio();
            WordsGame.Instance.RecallOrShuffle();
        }
    }
    #endregion
}