using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Gpm.Ui;

namespace BFX
{
    public class InteractionUI : UIBase
    {
        public static InteractionUI Instance => UIManager.HasInstance ? UIManager.Instance.GetCachedUI<InteractionUI>(UIList.InteractionUI) : null;

        [SerializeField] private InfiniteScroll infiniteScroll;

        private readonly List<InteractionContextData> dataContexts = new();
        private readonly Dictionary<string, InteractionUI_ListDataIdGroup> stackedUIMap = new();
        private readonly List<InteractionUI_ListData> allListData = new();
        private int currentSelectionIndex = -1;

        private void OnEnable()
        {
            InteractionHandler.OnInteractionTargetAdded -= HandleInteractionTargetAdded;
            InteractionHandler.OnInteractionTargetRemoved -= HandleInteractionTargetRemoved;
            InteractionHandler.OnInteractionTargetChanged -= HandleInteractionTargetChanged;
            InteractionHandler.OnInteractionTargetAdded += HandleInteractionTargetAdded;
            InteractionHandler.OnInteractionTargetRemoved += HandleInteractionTargetRemoved;
            InteractionHandler.OnInteractionTargetChanged += HandleInteractionTargetChanged;

            ClearData();
            InteractionSensor.RefreshAllActiveSensors(republishExisting: true);
        }

        private void OnDisable()
        {
            InteractionHandler.OnInteractionTargetAdded -= HandleInteractionTargetAdded;
            InteractionHandler.OnInteractionTargetRemoved -= HandleInteractionTargetRemoved;
            InteractionHandler.OnInteractionTargetChanged -= HandleInteractionTargetChanged;
        }

        private void HandleInteractionTargetAdded(InteractionContextData context)
        {
            if (context == null)
            {
                return;
            }

            AddInteractionData(context);
        }

        private void HandleInteractionTargetRemoved(InteractionContextData context)
        {
            if (context == null)
            {
                return;
            }

            RemoveInteractionData(context);
        }

        // 목록에 이미 표시 중인 대상의 잠금 상태 등 속성이 바뀌었을 때, 추가/제거 애니메이션 없이 표시만 갱신합니다.
        private void HandleInteractionTargetChanged(InteractionContextData context)
        {
            if (context == null)
            {
                return;
            }

            InteractionUI_ListData listData = allListData.FirstOrDefault(x => x.ContextKey == context.ContextKey);
            if (listData == null)
            {
                return;
            }

            listData.IsLocked = context.IsLocked;
            infiniteScroll.UpdateData(listData);
        }

        private void Awake()
        {
            if (infiniteScroll?.itemPrefab != null)
            {
                infiniteScroll.itemPrefab.gameObject.SetActive(false);
            }
        }

        public void Initialized() { }

        public void Initialize(PlayerController playerController) { }

        public void OnItemClicked(InteractionUI_ListItem item)
        {
            if (item == null)
            {
                return;
            }

            InteractionUI_ListData data = item.Data;
            if (data == null)
            {
                return;
            }

            TrySelectById(data.ID, data.ContextKey, data.IsStackable);
        }

        public void OnItemHovered(InteractionUI_ListItem item)
        {
            if (item == null || item.Data == null || infiniteScroll == null)
            {
                return;
            }

            var list = infiniteScroll.GetDataList();
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item.Data))
                {
                    SelectIndex(i, moveScrollToSelection: false);
                    return;
                }
            }
        }

        public void MoveSelectionByNavigate(Vector2 navigate)
        {
            if (Mathf.Abs(navigate.y) >= Mathf.Abs(navigate.x) && Mathf.Abs(navigate.y) > 0.5f)
            {
                MoveSelection(navigate.y);
            }
        }

        public Action GetTrySelectAction()
        {
            return TrySelect;
        }

        public bool TryInteractSelected()
        {
            if (infiniteScroll == null)
            {
                return false;
            }

            var list = infiniteScroll.GetDataList();
            if (currentSelectionIndex < 0 || currentSelectionIndex >= list.Count)
            {
                return false;
            }

            TrySelect();
            return true;
        }

        private void TrySelect()
        {
            if (currentSelectionIndex < 0)
                return;

            var list = infiniteScroll.GetDataList();
            if (currentSelectionIndex >= list.Count)
                return;

            if (list[currentSelectionIndex] is not InteractionUI_ListData selected)
                return;

            TrySelectById(selected.ID, selected.ContextKey, selected.IsStackable);
        }

        private void TrySelectById(string id, string contextKey = null, bool isStackable = true)
        {
            dataContexts.RemoveAll(x => !x.IsValid);
            var copiedList = new List<InteractionContextData>(dataContexts);
            foreach (var context in copiedList)
            {
                if (context.ID != id)
                {
                    continue;
                }

                if (!isStackable && context.ContextKey != contextKey)
                {
                    continue;
                }

                context.Execute();

                if (context.ShouldRemoveAfterUse)
                {
                    RemoveInteractionData(context);
                }

                if (!isStackable)
                {
                    break;
                }
            }
        }

        private void RefreshSelection()
        {
            SyncInfiniteScroll();
        }

        private void SyncInfiniteScroll()
        {
            var scrollList = infiniteScroll.GetDataList();
            var targetList = allListData.Take(5).ToList();

            for (int i = scrollList.Count - 1; i >= 0; i--)
            {
                if (!targetList.Contains(scrollList[i] as InteractionUI_ListData))
                {
                    infiniteScroll.RemoveData(i);
                }
            }

            scrollList = infiniteScroll.GetDataList();
            foreach (var target in targetList)
            {
                if (!scrollList.Contains(target))
                {
                    infiniteScroll.InsertData(target, true); // immediately
                }
                else
                {
                    infiniteScroll.UpdateData(target);
                }
            }

            // InteractionUI 전체 갱신
            infiniteScroll.UpdateAllData(true);

            var updatedScrollList = infiniteScroll.GetDataList();
            if (updatedScrollList.Count == 0)
            {
                currentSelectionIndex = -1;
            }
            else if (currentSelectionIndex >= updatedScrollList.Count)
            {
                currentSelectionIndex = updatedScrollList.Count - 1;
            }
            else if (currentSelectionIndex < 0)
            {
                currentSelectionIndex = 0;
            }

            if (currentSelectionIndex >= 0 && currentSelectionIndex < updatedScrollList.Count)
            {
                var newSelected = updatedScrollList[currentSelectionIndex] as InteractionUI_ListData;
                foreach (var item in updatedScrollList)
                {
                    if (item is InteractionUI_ListData data)
                    {
                        data.IsSelected = data == newSelected;
                        infiniteScroll.UpdateData(data);
                    }
                }
            }
        }

        private void MoveSelection(float direction)
        {
            var list = infiniteScroll.GetDataList();
            if (list.Count == 0 || Mathf.Approximately(direction, 0f))
                return;

            if (currentSelectionIndex >= 0 && currentSelectionIndex < list.Count)
            {
                if (list[currentSelectionIndex] is InteractionUI_ListData prev)
                {
                    prev.IsSelected = false;
                    infiniteScroll.UpdateData(prev);
                }
            }

            currentSelectionIndex += direction < 0f ? 1 : -1;

            if (currentSelectionIndex < 0)
                currentSelectionIndex = list.Count - 1;
            else if (currentSelectionIndex >= list.Count)
                currentSelectionIndex = 0;

            if (list[currentSelectionIndex] is InteractionUI_ListData next)
            {
                next.IsSelected = true;
                infiniteScroll.UpdateData(next);
                infiniteScroll.MoveToFromDataIndex(currentSelectionIndex, InfiniteScroll.MoveToType.MOVE_TO_CENTER, 0.1f);
            }
        }

        private void SelectIndex(int index, bool moveScrollToSelection)
        {
            var list = infiniteScroll.GetDataList();
            if (index < 0 || index >= list.Count || currentSelectionIndex == index)
            {
                return;
            }

            if (currentSelectionIndex >= 0 && currentSelectionIndex < list.Count &&
                list[currentSelectionIndex] is InteractionUI_ListData prev)
            {
                prev.IsSelected = false;
                infiniteScroll.UpdateData(prev);
            }

            currentSelectionIndex = index;

            if (list[currentSelectionIndex] is InteractionUI_ListData next)
            {
                next.IsSelected = true;
                infiniteScroll.UpdateData(next);
                if (moveScrollToSelection)
                {
                    infiniteScroll.MoveToFromDataIndex(currentSelectionIndex, InfiniteScroll.MoveToType.MOVE_TO_CENTER, 0.1f);
                }
            }
        }

        public void AddInteractionData(InteractionContextData context)
        {
            if (context == null || !context.IsValid || !context.IsInteractable)
            {
                return;
            }

            if (dataContexts.Any(x => x.ContextKey == context.ContextKey))
            {
                return;
            }

            dataContexts.Add(context);

            if (context.IsStackable)
            {
                AddStackableListData(context);
            }
            else
            {
                AddSingleListData(context);
            }

            EnsureSelectionExists();
            RefreshSelection();
        }

        private void AddStackableListData(InteractionContextData context)
        {
            if (stackedUIMap.TryGetValue(context.ID, out InteractionUI_ListDataIdGroup existingGroup))
            {
                existingGroup.DataList[0].StackCount += context.StackCount;
                return;
            }

            InteractionUI_ListData listData = CreateListData(context, () => TrySelectById(context.ID));
            listData.StackCount = context.StackCount;

            stackedUIMap.Add(context.ID, new InteractionUI_ListDataIdGroup(listData));
            allListData.Add(listData);
        }

        private void AddSingleListData(InteractionContextData context)
        {
            InteractionUI_ListData listData = CreateListData(
                context,
                () => TrySelectById(context.ID, context.ContextKey, false));
            listData.IsStackable = false;

            if (!stackedUIMap.TryGetValue(context.ID, out InteractionUI_ListDataIdGroup group))
            {
                stackedUIMap.Add(context.ID, new InteractionUI_ListDataIdGroup(listData));
            }
            else
            {
                group.DataList.Add(listData);
            }

            allListData.Add(listData);
        }

        private InteractionUI_ListData CreateListData(InteractionContextData context, Action onSelected)
        {
            return new InteractionUI_ListData(
                context.ID,
                context.ContextKey,
                context.Target.GetThumbnail(),
                context.Target.GetDescription(),
                onSelected)
            {
                IsSelected = allListData.Count == 0,
                IsLocked = context.IsLocked
            };
        }

        private void EnsureSelectionExists()
        {
            if (currentSelectionIndex < 0 && allListData.Count > 0)
            {
                currentSelectionIndex = 0;
            }
        }

        // 특정 ContextKey의 InteractionData remove
        public void RemoveInteractionData(string contextKey)
        {
            var interactionDataContext = dataContexts.FirstOrDefault(x => x.ContextKey == contextKey);
            if (interactionDataContext == null) return;
            RemoveInteractionData(interactionDataContext);
        }

        public void RemoveInteractionData(InteractionContextData context)
        {
            if (context == null)
            {
                return;
            }

            var trackedContext = dataContexts.FirstOrDefault(x => x.ContextKey == context.ContextKey);
            var removeContext = trackedContext ?? context;

            if (!stackedUIMap.ContainsKey(removeContext.ID))
                return;

            var list = infiniteScroll.GetDataList();
            
            if (currentSelectionIndex >= 0 && currentSelectionIndex < list.Count)
            {
                if (list[currentSelectionIndex] is InteractionUI_ListData { IsSelected: true } selected)
                {
                    selected.IsSelected = false;
                    infiniteScroll.UpdateData(selected);
                }
            }

            if (removeContext.IsStackable)
            {
                RemoveStackableListData(removeContext);
            }
            else
            {
                RemoveSingleListData(removeContext);
            }

            RefreshSelection();
        }

        private void RemoveStackableListData(InteractionContextData context)
        {
            if (!stackedUIMap.TryGetValue(context.ID, out InteractionUI_ListDataIdGroup group))
            {
                return;
            }

            dataContexts.RemoveAll(x => x.ContextKey == context.ContextKey);

            InteractionUI_ListData listData = group.DataList[0];
            listData.StackCount -= context.StackCount;
            if (listData.StackCount > 0)
            {
                return;
            }

            stackedUIMap.Remove(context.ID);
            allListData.Remove(listData);
        }

        private void RemoveSingleListData(InteractionContextData context)
        {
            if (!stackedUIMap.TryGetValue(context.ID, out InteractionUI_ListDataIdGroup group))
            {
                return;
            }

            InteractionUI_ListData listData = allListData.FirstOrDefault(x => x.ContextKey == context.ContextKey);
            if (listData == null)
            {
                return;
            }

            dataContexts.RemoveAll(x => x.ContextKey == context.ContextKey);
            group.DataList.Remove(listData);
            allListData.Remove(listData);

            if (group.DataList.Count <= 0)
            {
                stackedUIMap.Remove(context.ID);
            }
        }

        public void ClearData()
        {
            dataContexts.Clear();
            stackedUIMap.Clear();
            allListData.Clear();

            if (infiniteScroll != null)
            {
                infiniteScroll.ClearData(true);
            }

            currentSelectionIndex = -1;
        }
    }
}
