using UnityEngine;

namespace BFX
{
    /// <summary>
    /// UI와 매니저가 상호작용 항목을 표시하고 실행하기 위해 사용되는 컨텍스트입니다.
    /// 원본 데이터, 실행 대상, UI 그룹 ID, 개수 표시 정보를 함께 보관합니다.
    /// </summary>
    public class InteractionContextData
    {
        /// <summary>
        /// UI 표시와 실행에 사용할 상호작용 대상입니다.
        /// </summary>
        public IInteractionTarget Target { get; }

        /// <summary>
        /// UI에서 같은 종류의 상호작용을 묶기 위한 그룹 ID입니다.
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// 특정 타겟의 특정 데이터 항목을 구분하기 위한 고유 컨텍스트 키입니다.
        /// </summary>
        public string ContextKey { get; }

        /// <summary>
        /// UI에서 같은 ID를 가진 항목을 누적 표시할 수 있는지 나타냅니다.
        /// </summary>
        public bool IsStackable { get; }

        /// <summary>
        /// UI에 누적 표시할 개수입니다.
        /// </summary>
        public int StackCount { get; }

        /// <summary>
        /// 실행 후 UI 항목을 즉시 제거해야 하는지 나타냅니다.
        /// </summary>
        public bool ShouldRemoveAfterUse { get; }

        /// <summary>
        /// 상호작용 대상이 존재하는지 확인합니다.
        /// </summary>
        public bool IsValid => Target != null;

        /// <summary>
        /// 현재 원본 데이터가 상호작용 가능한 상태인지 확인합니다.
        /// </summary>
        public bool IsInteractable => Target != null && Target.IsInteractable;

        /// <summary>
        /// 상호작용 대상이 잠금 가능한 타입이면서, 현재 잠겨있는 상태인지 확인합니다.
        /// </summary>
        public bool IsLocked => Target is ILockableInteraction lockable && lockable.IsLocked;

        /// <summary>
        /// 상호작용 UI 항목과 실행 핸들에 필요한 컨텍스트 정보를 생성합니다.
        /// </summary>
        public InteractionContextData(
            IInteractionTarget target,
            string id = null,
            string contextKey = null,
            bool isStackable = false,
            int stackCount = 1,
            bool shouldRemoveAfterUse = false)
        {
            Target = target;

            ID = string.IsNullOrWhiteSpace(id)
                ? target?.GetInteractionType().ToString() ?? string.Empty
                : id;

            ContextKey = string.IsNullOrWhiteSpace(contextKey)
                ? target?.InstanceID ?? string.Empty
                : contextKey;

            IsStackable = isStackable;
            StackCount = Mathf.Max(1, stackCount);
            ShouldRemoveAfterUse = shouldRemoveAfterUse;
        }

        /// <summary>
        /// 현재 컨텍스트가 유효하고 상호작용 가능한 경우 타겟에게 실행을 위임합니다.
        /// </summary>
        public void Execute()
        {
            if (!IsValid || !IsInteractable || IsLocked)
            {
                return;
            }

            Target.Interact();
        }
    }
}
