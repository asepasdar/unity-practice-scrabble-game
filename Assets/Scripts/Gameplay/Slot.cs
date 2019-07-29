using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace Scrabble.Logic
{
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
                if (!isDefaultSlot) //Jika huruf di drop ke slot grid 15x15 dan Jika slot grid 15x15 kosong
                {
                    if (!item) //Jika slot grid 15x15 kosong
                    {
                        MyControll.draggedObject.transform.SetParent(transform);
                    }
                    else if (item && !item.GetComponent<MyControll>().canDrag)
                    {
                        MyControll.draggedObject.GetComponent<MyControll>().setToDefault();
                    }
                    else
                    {
                        MyControll.draggedObject.transform.SetParent(transform);
                        item.GetComponent<MyControll>().setToDefault();
                    }
                    MyControll Itemcontroll = item.GetComponent<MyControll>();
                    ExecuteEvents.ExecuteHierarchy<IHasChanged>(gameObject, null, (x, y) => x.hasChanged());
                    WordsGame.Instance.grid[row][col] = Itemcontroll;
                    WordsGame.Instance.wordSet[Itemcontroll.urutan][0] = row;
                    WordsGame.Instance.wordSet[Itemcontroll.urutan][1] = col;
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
}