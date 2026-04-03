using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Core
{
    public class Prefab_LeaderPanel : MonoBehaviour
    {
        #region Singleton

        private static Prefab_LeaderPanel _instance;

        /// <summary>
        /// Singleton instance with Unity-compliant lazy initialization.
        /// </summary>
        public static Prefab_LeaderPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<Prefab_LeaderPanel>();

                    if (_instance == null)
                    {
                        GameObject go = new("Prefab_LeaderPanel");
                        _instance = go.AddComponent<Prefab_LeaderPanel>();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Inspector Fields

        [Header("Leader Fields")]
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Image _rankImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _gradeText;
        [SerializeField] private TextMeshProUGUI _commandText;
        [SerializeField] private TextMeshProUGUI _reputationText;
        [SerializeField] private TextMeshProUGUI _tier1Text;
        [SerializeField] private TextMeshProUGUI _tier2Text;
        [SerializeField] private TextMeshProUGUI _tier3Text;
        [SerializeField] private TextMeshProUGUI _tier4Text;
        [SerializeField] private TextMeshProUGUI _tier5Text;

        #endregion // Inspector Fields

        #region Initialization

        /// <summary>
        /// Initializes the leader panel by verifying that all required UI components are assigned.
        /// </summary>
        public bool Initialize()
        {
            string errorString = "";

            if (_portraitImage == null) errorString += "Portrait Image is not assigned. ";
            if (_rankImage == null) errorString += "Rank Image is not assigned. ";
            if (_nameText == null) errorString += "Name Text is not assigned. ";
            if (_gradeText == null) errorString += "Grade Text is not assigned. ";
            if (_commandText == null) errorString += "Command Text is not assigned. ";
            if (_reputationText == null) errorString += "Reputation Text is not assigned. ";
            if (_tier1Text == null) errorString += "Tier 1 Text is not assigned. ";
            if (_tier2Text == null) errorString += "Tier 2 Text is not assigned. ";
            if (_tier3Text == null) errorString += "Tier 3 Text is not assigned. ";
            if (_tier4Text == null) errorString += "Tier 4 Text is not assigned. ";
            if (_tier5Text == null) errorString += "Tier 5 Text is not assigned. ";

            if (!string.IsNullOrEmpty(errorString))
            {
                Debug.LogError($"[Prefab_LeaderPanel] Initialization errors found: {errorString}");
                return false;
            }

            return true;
        }

        #endregion // Initialization

        #region Leader Panel Update

        /// <summary>
        /// Updates the leader panel with data from the currently selected leader.
        /// </summary>
        public void UpdateLeaderPanel()
        {
            var leader = GameDataManager.SelectedLeader;
            if (leader == null)
                return;

            // Portrait
            SetImage(_portraitImage, leader.PortraitId);

            // Rank board image based on command grade
            SetImage(_rankImage, GetRankBoardSpriteName(leader.CommandGrade));

            // Name: "FormattedRank Name"
            SetText(_nameText, $"{leader.FormattedRank} {leader.Name}");

            // Grade
            SetText(_gradeText, leader.CommandGrade switch
            {
                CommandGrade.JuniorGrade => "Junior",
                CommandGrade.SeniorGrade => "Senior",
                CommandGrade.TopGrade => "Top",
                _ => leader.CommandGrade.ToString()
            });

            // Command ability
            SetText(_commandText, leader.CombatCommand.ToString());

            // Reputation points
            SetText(_reputationText, leader.ReputationPoints.ToString());

            // Tier skill counts
            SetText(_tier1Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier1).ToString());
            SetText(_tier2Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier2).ToString());
            SetText(_tier3Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier3).ToString());
            SetText(_tier4Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier4).ToString());
            SetText(_tier5Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier5).ToString());
        }

        #endregion // Leader Panel Update

        #region Helper Methods

        /// <summary>
        /// Returns the rank board sprite name for a given command grade.
        /// </summary>
        private string GetRankBoardSpriteName(CommandGrade grade)
        {
            return grade switch
            {
                CommandGrade.JuniorGrade => SpriteManager.Colonel,
                CommandGrade.SeniorGrade => SpriteManager.MajorGeneral,
                CommandGrade.TopGrade => SpriteManager.LieutenantGeneral,
                _ => SpriteManager.Colonel
            };
        }

        /// <summary>
        /// Sets a TextMeshProUGUI text value with null safety.
        /// </summary>
        private void SetText(TextMeshProUGUI textField, string value)
        {
            if (textField != null)
                textField.text = value;
        }

        /// <summary>
        /// Sets an Image sprite by name using SpriteManager, with null safety.
        /// </summary>
        private void SetImage(Image imageField, string spriteName)
        {
            if (imageField == null || string.IsNullOrEmpty(spriteName))
                return;

            var sprite = SpriteManager.GetSprite(spriteName);
            if (sprite != null)
                imageField.sprite = sprite;
        }

        #endregion // Helper Methods
    }
}
