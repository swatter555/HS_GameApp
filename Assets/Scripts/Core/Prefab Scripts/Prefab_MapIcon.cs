using HammerAndSickle.Core.GameData;
using UnityEngine;

namespace HammerAndSickle.Core
{
    public class Prefab_MapIcon : MonoBehaviour
    {
        #region Inspector Fields

        [SerializeField]
        private MapIconType IconType;
        [SerializeField]
        private Vector2Int Position;
        [SerializeField]
        private SpriteRenderer SpriteRenderer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type of icon.
        /// </summary>
        /// <returns></returns>
        public MapIconType GetIconType()
        {
            return IconType;
        }

        /// <summary>
        /// Gets the position of the icon.
        /// </summary>
        /// <returns></returns>
        public Vector2Int GetPosition()
        {
            return Position;
        }

        /// <summary>
        /// Gets the sprite renderer of the icon.
        /// </summary>
        /// <returns></returns>
        public SpriteRenderer GetSpriteRenderer()
        {
            return SpriteRenderer;
        }

        /// <summary>
        /// Sets the type of icon.
        /// </summary>
        /// <param name="iconType"></param>
        public void SetIconType(MapIconType iconType)
        {
            IconType = iconType;
        }

        /// <summary>
        /// Sets the position of the icon.
        /// </summary>
        /// <param name="position"></param>
        public void SetPosition(Vector2Int position)
        {
            Position = position;
        }

        /// <summary>
        /// Sets the sprite renderer of the icon.
        /// </summary>
        /// <param name="spriteRenderer"></param>
        public void SetSpriteRenderer(SpriteRenderer spriteRenderer)
        {
            SpriteRenderer = spriteRenderer;
        }

        #endregion
    }
}