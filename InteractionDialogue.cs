using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace BFX
{
    /// <summary>
    /// NPC 또는 오브젝트와의 대화를 시작하는 상호작용 타겟입니다.
    /// Dialogue System 대화 시작과 대화 초상화 적용을 담당합니다.
    /// </summary>
    public class InteractionDialogue : MonoBehaviour, IInteractionTarget
    {
        [SerializeField] private NpcIdentity npcIdentity;
        [SerializeField] private Sprite interactionThumbnail;
        [SerializeField] private Sprite npcThumbnail;
        [SerializeField] private InteractionDescriptionType descriptionType = InteractionDescriptionType.Dialogue;

        // Dialogue System에서 시작할 Conversation 제목.
        [SerializeField] private string conversationTitle;

        // 초상화 적용 시 사용할 Dialogue System Actor 이름.
        [SerializeField] private string portraitActorName;

        [SerializeField] private Transform actor;
        [SerializeField] private Transform conversant;

        // 특정 Entry에서 대화를 시작해야 할 때 사용할 Dialogue Entry ID.
        [SerializeField] private int initialDialogueEntryID;

        [SerializeField] private bool useThumbnailAsPortrait = true;
        [SerializeField] private bool isInteractable = true;

        /// <summary>
        /// 현재 대화 상호작용이 가능한 상태인지 나타냅니다.
        /// </summary>
        public bool IsInteractable
        {
            get => isInteractable;
            set => isInteractable = value;
        }

        public string InteractionId => npcIdentity != null ? npcIdentity.NpcId : string.Empty;
        public string InstanceID => $"target:{GetInstanceID()}";
        public bool IsStackable => false;
        public int StackCount => 1;

        /// <summary>
        /// UI에 표시할 상호작용 썸네일을 반환합니다.
        /// </summary>
        public Sprite GetThumbnail() => interactionThumbnail;

        /// <summary>
        /// UI에 표시할 대화 설명을 반환합니다.
        /// </summary>
        public string GetDescription() => descriptionType.ToDisplayText();

        /// <summary>
        /// 이 타겟이 제공하는 상호작용 타입을 반환합니다.
        /// </summary>
        public InteractionType GetInteractionType() => InteractionType.Dialogue;

        /// <summary>
        /// Conversation 제목을 외부 데이터로 초기화한다.
        /// </summary>
        public void Init(string newConversationTitle)
        {
            if (!string.IsNullOrWhiteSpace(newConversationTitle))
            {
                conversationTitle = newConversationTitle;
            }

            if (isActiveAndEnabled)
            {
                InteractionSensor.RefreshAllActiveSensors();
            }
        }

        /// <summary>
        /// 컴포넌트 초기 상태를 보정한다.
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
            EnsureTriggerCollider();
        }

        /// <summary>
        /// 활성화될 때 Collider 설정을 보정하고 base의 자동 등록/센서 갱신을 실행합니다.
        /// </summary>
        private void OnEnable()
        {
            EnsureTriggerCollider();
            InteractionSensor.RefreshAllActiveSensors();
        }

        private void OnDisable()
        {
            InteractionSensor.RefreshAllActiveSensors();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 값이 변경될 때 Collider 설정을 Trigger로 유지합니다.
        /// </summary>
        private void OnValidate()
        {
            ResolveReferences();
            EnsureTriggerCollider();
        }
#endif

        /// <summary>
        /// 설정된 대화 제목과 Actor 정보를 사용해 Dialogue System 대화를 시작합니다.
        /// </summary>
        public void Interact()
        {
            // 상호작용 가능 상태 검사.
            if (!IsInteractable)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(conversationTitle))
            {
                Debug.LogWarning($"{nameof(InteractionDialogue)}: conversationTitle is empty.", this);
                return;
            }

            if (DialogueManager.instance == null)
            {
                Debug.LogWarning($"{nameof(InteractionDialogue)}: Dialogue Manager is not in the scene.", this);
                return;
            }

            // Actor와 Conversant 보정.
            Transform resolvedActor = actor != null ? actor : FindActor();
            Transform resolvedConversant = conversant != null ? conversant : transform;

            // NPC 초상화 반영.
            ApplyPortrait(resolvedConversant);

            // 지정 Entry 또는 기본 START 진입.
            if (initialDialogueEntryID > 0)
            {
                DialogueManager.StartConversation(
                    conversationTitle,
                    resolvedActor,
                    resolvedConversant,
                    initialDialogueEntryID);
            }
            else
            {
                DialogueManager.StartConversation(
                    conversationTitle,
                    resolvedActor,
                    resolvedConversant);
            }
        }

        /// <summary>
        /// 대화 상대에게 DialogueActor를 보장하고, 설정된 초상화 정보를 적용합니다.
        /// </summary>
        private void ApplyPortrait(Transform target)
        {
            if (!useThumbnailAsPortrait || npcThumbnail == null || target == null)
            {
                return;
            }

            DialogueActor dialogueActor = target.GetComponent<DialogueActor>();
            if (dialogueActor == null)
            {
                dialogueActor = target.gameObject.AddComponent<DialogueActor>();
            }

            if (!string.IsNullOrWhiteSpace(portraitActorName))
            {
                dialogueActor.actor = portraitActorName;
            }
            else if (string.IsNullOrWhiteSpace(dialogueActor.actor))
            {
                dialogueActor.actor = target.name;
            }

            if (dialogueActor.spritePortrait == null || dialogueActor.spritePortrait == npcThumbnail)
            {
                dialogueActor.spritePortrait = npcThumbnail;
            }
        }

        /// <summary>
        /// 명시된 Actor가 없을 때 씬에서 첫 InteractionSensor를 찾아 대화 주체로 사용합니다.
        /// </summary>
        private static Transform FindActor()
        {
            InteractionSensor sensor = FindFirstObjectByType<InteractionSensor>();
            return sensor != null ? sensor.transform : null;
        }

        /// <summary>
        /// 부모 NPC의 식별 정보를 자동으로 찾는다.
        /// </summary>
        private void ResolveReferences()
        {
            if (npcIdentity == null)
            {
                npcIdentity = GetComponentInParent<NpcIdentity>();
            }
        }

        /// <summary>
        /// 타겟 Collider가 존재하면 Trigger로 설정해 센서 진입/이탈 이벤트를 받을 수 있게 합니다.
        /// </summary>
        private void EnsureTriggerCollider()
        {
            Collider targetCollider = GetComponent<Collider>();
            if (targetCollider != null)
            {
                targetCollider.isTrigger = true;
            }
        }
    }
}
