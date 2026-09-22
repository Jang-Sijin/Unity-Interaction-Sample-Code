using System.Collections.Generic;
using DG.Tweening;
using Gpm.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BFX
{
    public class InteractionUI_ListData : InfiniteScrollData
    {
        public InteractionUI_ListData(string id, string contextKey, Sprite iconSprite, string message, System.Action onInteract)
        {
            ID = id;
            ContextKey = contextKey;
            Icon = iconSprite;
            Message = message;
            Action = onInteract;
        }

        public string ID { get; private set; }
        public string ContextKey { get; private set; }
        public bool IsSelected { get; set; }
        public bool IsStackable { get; set; } = true;
        public int StackCount { get; set; } = 1;
        public Sprite Icon { get; private set; }
        public string Message { get; private set; }
        public System.Action Action { get; private set; }
        public bool IsLocked { get; set; }
    }

    public class InteractionUI_ListDataIdGroup
    {
        public List<InteractionUI_ListData> DataList = new List<InteractionUI_ListData>();

        public InteractionUI_ListDataIdGroup(InteractionUI_ListData data)
        {
            DataList.Add(data);
        }
    }

    public class InteractionUI_ListItem : InfiniteScrollItem, IPointerEnterHandler
    {
        public bool IsSelected
        {
            set
            {
                if (selection != null)
                {
                    selection.SetActive(value);
                }
            }
        }
        public InteractionUI_ListData Data => currentData;

        [SerializeField] private InteractionUI parent;
        [SerializeField] private Image actionIcon;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private GameObject selection;

        [Header("잠금 표시 설정")]
        [SerializeField] private Sprite lockedIcon;
        [SerializeField] private Color lockedTint = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private string lockedMessage = "잠김";

        private static readonly Color UnlockedTint = Color.white;

        private DOTweenAnimation tweenTimeline;
        private InteractionUI_ListData currentData;

        private void Awake()
        {
            tweenTimeline = GetComponent<DOTweenAnimation>();
            if (parent == null)
            {
                parent = GetComponentInParent<InteractionUI>();
            }
        }

        private void OnEnable()
        {
            if (tweenTimeline?.tween?.onComplete != null)
            {
                tweenTimeline.tween.onComplete = null;
                tweenTimeline.tween.Rewind();
            }
        }

        public override void UpdateData(InfiniteScrollData scrollData)
        {
            currentData = scrollData as InteractionUI_ListData;
            if (currentData == null)
                return;

            bool isLocked = currentData.IsLocked;

            actionText.text = isLocked ? lockedMessage : currentData.Message;
            if (actionIcon != null)
            {
                actionIcon.sprite = isLocked && lockedIcon != null ? lockedIcon : currentData.Icon;
                actionIcon.enabled = actionIcon.sprite != null;
                actionIcon.color = isLocked ? lockedTint : UnlockedTint;
            }
            IsSelected = currentData.IsSelected;

            if (currentData.IsStackable && currentData.StackCount > 1)
            {
                countText.gameObject.SetActive(true);
                countText.text = $"x{currentData.StackCount}";
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }

        public void OnClick()
        {
            parent?.OnItemClicked(this);

            if (tweenTimeline == null)
            {
                return;
            }

            tweenTimeline.DOPlay();
            if (tweenTimeline.tween == null) return;

            tweenTimeline.tween.onComplete = null;
            tweenTimeline.tween.onComplete += () =>
            {
                if (tweenTimeline.tween != null)
                {
                    tweenTimeline.tween.Rewind(); // 연출 전 상태로 다시 복구
                    tweenTimeline.tween.onComplete = null;
                }
            };
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            parent?.OnItemHovered(this);
        }
    }
}
