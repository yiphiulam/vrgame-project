using UnityEngine;
using TheBreathlessStudyRoom.Core;
using System.Text;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// Manages Guard Room climax decisions and displays the VR Ending Billboard
    /// dynamically in front of the player's camera when the experience concludes.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Core/Ending Manager")]
    public class EndingManager : MonoBehaviour
    {
        [Header("Ending Panel UI")]
        [Tooltip("The parent GameObject of the ending billboard to enable.")]
        public GameObject endingBillboardPanel;

        [Tooltip("TextMesh component for displaying the ending title.")]
        public TextMesh titleText;

        [Tooltip("TextMesh component for displaying the ending description.")]
        public TextMesh descriptionText;

        [Tooltip("TextMesh component for displaying compliance, rebellion, and sanity stats.")]
        public TextMesh statsText;

        [Tooltip("TextMesh component representing the evaluation stamp (e.g. FREE, VOID).")]
        public TextMesh stampText;

        [Header("CCTV Screen reference to turn off")]
        [Tooltip("The CCTV screen mesh to turn black upon smash.")]
        public MeshRenderer cctvScreenRenderer;

        [Header("Emergency Lighting")]
        [Tooltip("Emergency red lighting to enable upon screen smash.")]
        public Light redEmergencyLight;

        [Header("Timeline Linkage")]
        [SerializeField] private TimelineManager _timelineManager;

        private bool _isEndingTriggered = false;

        private void Start()
        {
            if (_timelineManager == null)
            {
                _timelineManager = FindObjectOfType<TimelineManager>();
            }

            if (_timelineManager != null)
            {
                _timelineManager.OnTimelineTrigger += HandleTimelineTrigger;
            }

            if (endingBillboardPanel != null)
            {
                endingBillboardPanel.SetActive(false);
            }

            if (redEmergencyLight != null)
            {
                redEmergencyLight.intensity = 0f;
            }
        }

        private void OnDestroy()
        {
            if (_timelineManager != null)
            {
                _timelineManager.OnTimelineTrigger -= HandleTimelineTrigger;
            }
        }

        private void HandleTimelineTrigger(string eventTag)
        {
            if (string.Equals(eventTag, "SANITY_CRUSH_ENDING", System.StringComparison.OrdinalIgnoreCase))
            {
                TriggerEnding("SANITY_COLLAPSE");
            }
        }

        /// <summary>
        /// Triggered when the player smashes the monitor.
        /// </summary>
        public void TriggerSmashedMonitorEnding()
        {
            if (_isEndingTriggered) return;

            // Log choice to Timeline
            if (_timelineManager != null)
            {
                _timelineManager.LogRebellion(5);
            }

            // Visual shutter feedback
            if (cctvScreenRenderer != null)
            {
                cctvScreenRenderer.material.color = Color.black;
                cctvScreenRenderer.material.mainTexture = null;
                // If it has shader texture property, reset it too
                if (cctvScreenRenderer.material.HasProperty("_BaseMap"))
                {
                    cctvScreenRenderer.material.SetTexture("_BaseMap", null);
                }
            }

            // Emergency red lighting flash
            if (redEmergencyLight != null)
            {
                redEmergencyLight.intensity = 2.0f;
            }

            // Play jumpscare/scare sound
            var audioManager = FindObjectOfType<AudioManager>();
            if (audioManager != null)
            {
                audioManager.PlayJumpscareChord();
            }

            TriggerEnding("REBEL_ESCAPE");
        }

        /// <summary>
        /// Triggered when the player stamps the clipboard.
        /// </summary>
        public void TriggerStampedComplianceEnding()
        {
            if (_isEndingTriggered) return;

            // Log choice to Timeline
            if (_timelineManager != null)
            {
                _timelineManager.LogCompliance(5);
            }

            // Play audio click
            var audioManager = FindObjectOfType<AudioManager>();
            if (audioManager != null)
            {
                audioManager.PlayDoorSqueak(); // Reuse door sound for the mechanical stamp feedback
            }

            TriggerEnding("COMPLIANT_GEAR");
        }

        private void TriggerEnding(string endingType)
        {
            _isEndingTriggered = true;

            // Set state to Ending
            if (_timelineManager != null)
            {
                _timelineManager.SetGameState(GameState.Ending);
            }

            // Calculate final scores
            int compliance = _timelineManager != null ? _timelineManager.ComplianceCount : 0;
            int rebel = _timelineManager != null ? _timelineManager.RebellionCount : 0;
            int total = compliance + rebel;
            int complianceRate = total > 0 ? Mathf.RoundToInt((float)compliance / total * 100f) : 100;
            int rebelRate = 100 - complianceRate;
            int sanityRemaining = _timelineManager != null ? Mathf.Max(0, 100 - Mathf.RoundToInt(_timelineManager.StressLevel)) : 100;

            string endingTitle = "";
            string endingDesc = "";
            string stampVal = "";
            Color titleColor = Color.white;
            Color stampColor = Color.white;

            if (endingType == "SANITY_COLLAPSE")
            {
                endingTitle = "理智崩潰 (SANITY COLLAPSE)";
                titleColor = Color.red;
                endingDesc = "你在無處不在的矛盾規則與壓迫中失去了所有理智，心跳失控。自習室的黑暗吞噬了你。你淪落為這座自習室地底無名怨念的又一個影子，大腦一片空白，無法再次睜開雙眼。";
                stampVal = "作廢 (VOID)";
                stampColor = Color.red;
            }
            else if (endingType == "REBEL_ESCAPE")
            {
                endingTitle = "清醒的叛逆者 (THE AWAKE REBEL)";
                titleColor = Color.green;
                endingDesc = "在最後的關頭，你拒絕了「順從性測試」評估認證，一拳擊碎了監控自習室的主螢幕。隨後在玻璃碎片中，你找到了一條真正通往校園外圍的求生小路。雖然外面的深夜依舊冰冷，但你成功保全了自己的獨立思考意志，你，自由了。";
                stampVal = "自由 (FREE)";
                stampColor = Color.green;
            }
            else
            {
                // COMPLIANT_GEAR
                if (rebelRate >= 60)
                {
                    endingTitle = "妥協的清醒者 (COMPROMISED THINKER)";
                    titleColor = new Color(1.0f, 0.64f, 0f); // Amber
                    endingDesc = "雖然你一路尋找到了隱藏的真實守則，並敏銳地察覺到規則的欺騙性，但在警衛室那沉甸甸的審核公文與管理員警報器的巨壓下，你最終還是妥協屈服，親手蓋下了「順從」的認證印章。你帶著清醒的痛苦，自我麻痺，回到了冰冷課桌前。";
                    stampVal = "屈從認證";
                    stampColor = new Color(1.0f, 0.64f, 0f);
                }
                else
                {
                    endingTitle = "順從的螺絲釘 (COMPLIANT SLAVE)";
                    titleColor = Color.red;
                    endingDesc = "你毫無保留地服從了管理員下達的所有荒謬指令，將個人的邏輯與理智完全扼殺，以換取權威之下的安全感。恭喜你，你已成功被規訓為一名最完美的無聲螺絲釘，將在不見天日的自習室中，永遠安分地旋轉下去。";
                    stampVal = "完全服從";
                    stampColor = Color.red;
                }
            }

            // Update text meshes
            if (titleText != null)
            {
                titleText.text = endingTitle;
                titleText.color = titleColor;
            }

            if (descriptionText != null)
            {
                descriptionText.text = WordWrap(endingDesc, 22);
            }

            if (statsText != null)
            {
                statsText.text = $"順從率 (Obedience): {complianceRate}%\n叛逆率 (Rebellion): {rebelRate}%\n理智值 (Sanity): {sanityRemaining}%";
            }

            if (stampText != null)
            {
                stampText.text = stampVal;
                stampText.color = stampColor;
            }

            // Enable billboard and position it in front of player camera
            if (endingBillboardPanel != null)
            {
                endingBillboardPanel.SetActive(true);
                
                // Position billboard dynamically in front of main camera
                Transform camTrans = Camera.main != null ? Camera.main.transform : null;
                if (camTrans != null)
                {
                    Vector3 spawnPos = camTrans.position + camTrans.forward * 1.8f;
                    // Keep the height at a comfortable level (around y=1.2 to 1.4)
                    spawnPos.y = Mathf.Clamp(spawnPos.y, 1.0f, 1.6f);
                    
                    endingBillboardPanel.transform.position = spawnPos;
                    endingBillboardPanel.transform.LookAt(camTrans);
                    endingBillboardPanel.transform.Rotate(0, 180, 0); // Face player
                }
                
                Debug.Log($"[EndingManager] Spawned Ending Billboard Panel. Type: {endingType}");
            }
        }

        private string WordWrap(string input, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(input)) return "";
            StringBuilder sb = new StringBuilder();
            int count = 0;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                sb.Append(c);
                count++;
                
                if (count >= maxCharsPerLine)
                {
                    sb.Append("\n");
                    count = 0;
                }
            }
            return sb.ToString();
        }
    }
}
