using System;
using UnityEngine;

namespace BFX
{
    // 상호작용 UI와 실행 로직에서 사용하는 액션 종류
    public enum InteractionType // 상호작용 액션 타입 종류
    {
        None = 0,
        
        Dialogue,           // 대화 하기
        DropItem,           // 아이템 줍기
        DropBox,            // 아이템 상자 열기
        EnterPartySelect,   // UI를 켜기
        EnterBattleField,   // Scene 전환
        EnterBaseCamp,      // Scene 전환(베이스캠프로 복귀)
        SupplyBox,          // 파티원 탄약/체력/방어구 보급
        Lever,              // 레버를 당겨 기믹(다리 내리기 등)을 작동
        PushableBox,        // 상자를 밀기 시작
        Switch,             // 스위치를 눌러 기믹을 작동
        Panel,              // 패널을 조작해 기믹을 작동
        Generator,          // 발전기를 작동
        Ladder,             // 사다리 타기

        // 필요에 따라 게임에 맞는 타입을 추가하세요
    }

    // 상호작용 가능한 타입 정의
    public enum InteractionDescriptionType
    {
        None = 0,
        Dialogue,
        DropItem,
        DropBox,
        EnterPartySelect,
        EnterBattleField,
        EnterBaseCamp,
        SupplyBox,
        Lever,
        PushableBox,
        Switch,
        Panel,
        Generator,
        Ladder,
        Locked,             // 잠긴 트리거에 상호작용을 시도했을 때 표시

        // #필독: 신규 상호작용 추가 시, 기입 필요
    }

    public static class InteractionDescriptionTypeExtensions
    {
        public static string ToDisplayText(this InteractionDescriptionType descriptionType)
        {
            return descriptionType switch
            {
                InteractionDescriptionType.Dialogue => "대화",
                InteractionDescriptionType.DropItem => "아이템 줍기",
                InteractionDescriptionType.DropBox => "아이템 박스",
                InteractionDescriptionType.EnterPartySelect => "파티 선택",
                InteractionDescriptionType.EnterBattleField => "배틀필드 입장",
                InteractionDescriptionType.EnterBaseCamp => "베이스캠프 복귀",
                InteractionDescriptionType.SupplyBox => "보급상자",
                InteractionDescriptionType.Lever => "레버 당기기",
                InteractionDescriptionType.PushableBox => "상자 밀기",
                InteractionDescriptionType.Switch => "스위치 누르기",
                InteractionDescriptionType.Panel => "패널 조작하기",
                InteractionDescriptionType.Generator => "발전기 작동",
                InteractionDescriptionType.Ladder => "사다리 타기",
                InteractionDescriptionType.Locked => "잠김",
                _ => string.Empty,
            };
        }
    }
    
    // 센서가 충돌한 객체에서 읽는 최소 상호작용 식별 정보입니다.
    public interface IInteractionTarget
    {
        // 상호작용 데이터
        string InteractionId { get; }           // 상호작용 ID
        string InstanceID { get; }              // 객체의 인스턴스 ID
        bool IsStackable { get; }               // 해당 상호작용 객체가 UI에서 누적 가능한지 여부.
        int StackCount { get; }                 // UI에 누적 표시될 개수.
        bool IsInteractable { get; set; }       // 현재 데이터가 UI 표시와 실행 대상이 될 수 있는지 나타냅니다.
        InteractionType GetInteractionType();   // 이 데이터가 나타내는 상호작용 타입을 반환합니다.
        Sprite GetThumbnail();                  // UI에 표시할 썸네일을 반환합니다.
        string GetDescription();                // UI에 표시할 설명 문구를 반환합니다.
        
        // 추가할 것: 상호작용 객체에서 해당 Interface를 상속받아, Interact(어떤 액션을 수행하게 만들 것인지) 구현부 로직을 만들어야 합니다.
        // 무엇을 하는가: 을 UI에서 상호작용 이벤트를 발생시키면, 상호작용 객체에 해당되는 액션 로직들을 수행하도록 처리합니다.
        void Interact();
    }

}