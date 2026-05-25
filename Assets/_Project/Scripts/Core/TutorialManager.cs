using UnityEngine;
using TheBreathlessStudyRoom.Core;
using TheBreathlessStudyRoom.Interactables;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// 管理開場的線性教學流程：「拿手電筒 → 看規則 → 找出口」。
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Tooltip("負責生存倒數的時間軸管理器")]
        public TimelineManager timelineManager;
        [Tooltip("防卡關提示系統")]
        public HintSystem hintSystem;
        
        [Header("Tutorial State")]
        public bool isTutorialComplete = false;
        
        private int _tutorialStep = 0; // 0: 找手電筒, 1: 看黑板, 2: 找出口

        private void Start()
        {
            if (timelineManager != null)
            {
                // 一開始先暫停恐怖倒數，確保玩家能無壓力完成教學
                timelineManager.PauseTimeline();
                Debug.Log("[Tutorial] 教學開始。請先拿起手電筒。");
            }
        }

        /// <summary>
        /// 當手電筒被拿起來時由 FlashlightIntro 呼叫
        /// </summary>
        public void OnFlashlightGrabbed()
        {
            if (_tutorialStep == 0)
            {
                _tutorialStep = 1;
                Debug.Log("[Tutorial] 步驟一完成：已取得手電筒。現在引導玩家看黑板規則。");
                
                if (hintSystem != null && hintSystem.blackboardTarget != null)
                {
                    hintSystem.SetTarget(hintSystem.blackboardTarget);
                }
            }
        }

        /// <summary>
        /// 當玩家用 GazeDwellSelector 注視黑板超過指定時間後呼叫
        /// </summary>
        public void OnBlackboardLookedAt()
        {
            if (_tutorialStep == 1 && !isTutorialComplete)
            {
                _tutorialStep = 2;
                isTutorialComplete = true;
                Debug.Log("[Tutorial] 步驟二完成：已閱讀規則。解除暫停，5 分鐘生存計時正式開始！");
                
                if (timelineManager != null)
                {
                    timelineManager.ResumeTimeline();
                }

                if (hintSystem != null && hintSystem.doorTarget != null)
                {
                    hintSystem.SetTarget(hintSystem.doorTarget); // 下一個目標是門口
                }
            }
        }
    }
}
