using System;
using UnityEngine;

namespace BFX
{
    /// <summary>
    /// 상호작용 시스템 이벤트와 타겟 식별 가능하도록 핸들링을 처리하는 부분.
    /// 실제 충돌 감지와 UI 컨텍스트 생성은 InteractionSensor가 담당합니다.
    /// </summary>
    public static class InteractionHandler
    {
        public static event Action<InteractionContextData> OnInteractionTargetAdded;
        public static event Action<InteractionContextData> OnInteractionTargetRemoved;
        public static event Action<InteractionContextData> OnInteractionTargetChanged;

        public static void NotifyTargetAdded(InteractionContextData context)
        {
            OnInteractionTargetAdded?.Invoke(context);
        }

        public static void NotifyTargetRemoved(InteractionContextData context)
        {
            OnInteractionTargetRemoved?.Invoke(context);
        }

        // 목록에 이미 표시 중인 대상의 속성(예: 잠금 상태)이 바뀌었을 때 호출합니다.
        public static void NotifyTargetChanged(InteractionContextData context)
        {
            OnInteractionTargetChanged?.Invoke(context);
        }
    }
}
