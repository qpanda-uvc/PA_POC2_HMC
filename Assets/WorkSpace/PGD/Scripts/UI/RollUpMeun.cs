using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PGD
{
    public class RollUpMeun : MonoBehaviour
    {
        [SerializeField] private List<RectTransform> button_leftButtonList = new List<RectTransform>();
        [SerializeField] private RectTransform bg;
        [SerializeField] private GameObject buttons;

        private bool isDown = false;
        private bool isViable = true;

        void Start()
        {
            bg = gameObject.transform.GetChild(0).GetComponent<RectTransform>();

            for (int i = 0; i < gameObject.transform.GetChild(1).childCount; i++)
            {
                button_leftButtonList.Add(gameObject.transform.GetChild(1).GetChild(i).GetComponent<RectTransform>());
            }
        }

        public void OnClickRollUpMenuBtn()
        {
            if (!isViable)
                return;

            isViable = false;
            isDown = !isDown;

            float bgMoveVaule = isDown ? -35f : 830f;

            //float buttonMoveValue = isDown ? -80f : 80f;
            //float bgEndPos = isDown ? -buttonMoveValue * button_leftButtonList.Count : 0f;
            bg.GetChild(0).DOLocalMoveY(bgMoveVaule, 0.2f).OnComplete(() => isViable = true);


            //bg.gameObject.SetActive(true);
            //buttons.SetActive(true);

            //float buttonMoveValue = isDown ? -80f : 80f;
            //float bgEndPos = isDown ? -buttonMoveValue * button_leftButtonList.Count : 0f;
            //float duration = isDown ? (0.1f - 0.02f) * (button_leftButtonList.Count - 0.5f) : (0.1f + 0.02f) * (button_leftButtonList.Count);
            //Sequence squence = DOTween.Sequence();

            //squence.Join(bg.DOSizeDelta(new Vector2(bg.sizeDelta.x, bgEndPos), duration).SetEase(Ease.Linear));

            //for (int i = 0; i < button_leftButtonList.Count; i++)
            //{
            //    float currentPosY = button_leftButtonList[i].anchoredPosition.y;

            //    if(i == 0)
            //    {
            //        squence.Join(button_leftButtonList[i].DOAnchorPosY(currentPosY, 0.1f).SetEase(Ease.Linear));
            //    }
            //    else
            //    {
            //        squence.Join(button_leftButtonList[i].DOAnchorPosY(currentPosY + buttonMoveValue, 0.1f).SetLoops(i, LoopType.Incremental).SetEase(Ease.Linear));
            //    }         
            //}

            //squence.AppendCallback(SquenceCallback);
        }

        private void SquenceCallback()
        {
            isViable = true;

            if (!isDown)
            {
                bg.gameObject.SetActive(false);
                buttons.SetActive(false);
            }
        }
    }
}

