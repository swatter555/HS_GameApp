using HammerAndSickle.Core.GameData;
using UnityEngine;

namespace HammerAndSickle.Core
{
    public class Prefab_BridgeIcon : MonoBehaviour
    {
        #region Inspector Fields

        [SerializeField] private BridgeType BridgeType;
        [SerializeField] private Vector2Int Position;
        [SerializeField] private HexDirection Direction;
        [SerializeField] private SpriteRenderer SpriteRenderer;

        #endregion // Inspector Fields

        #region Properties

        /// <summary>
        /// Gets or sets the type of bridge.
        /// </summary>
        public BridgeType Type
        {
            get => BridgeType;
            set => BridgeType = value;
        }

        /// <summary>
        /// Gets or sets the position of the bridge.
        /// </summary>
        public Vector2Int Pos
        {
            get => Position;
            set => Position = value;
        }

        /// <summary>
        /// Gets or sets the direction of the bridge.
        /// </summary>
        public HexDirection Dir
        {
            get => Direction;
            set => Direction = value;
        }

        /// <summary>
        /// Gets or sets the sprite renderer for the bridge icon.
        /// </summary>
        public SpriteRenderer Renderer
        {
            get => SpriteRenderer;
            set => SpriteRenderer = value;
        }

        #endregion // Properties
    }
}