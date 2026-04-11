using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Handles LeanTween-based hex-to-hex movement animation for combat unit icons.
    /// Provides per-step callbacks so MovementController can run spotting after each hex.
    /// </summary>
    public static class UnitMoveAnimator
    {
        private const string CLASS_NAME = nameof(UnitMoveAnimator);

        /// <summary>
        /// Animates a single hex step from current position to target world position.
        /// Fires onComplete when the tween finishes so MovementController can proceed.
        /// </summary>
        /// <param name="icon">The unit icon GameObject to animate.</param>
        /// <param name="to">Target world position (hex center).</param>
        /// <param name="duration">Duration in seconds (suggest 0.15-0.25 for ground, 0.08 for fixed-wing).</param>
        /// <param name="onComplete">Callback fired when animation completes.</param>
        public static void AnimateHexStep(GameObject icon, Vector3 to, float duration, Action onComplete)
        {
            try
            {
                if (icon == null)
                {
                    onComplete?.Invoke();
                    return;
                }

                LeanTween.move(icon, to, duration)
                    .setEaseInOutQuad()
                    .setOnComplete(() => onComplete?.Invoke());
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AnimateHexStep), e);
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Animates auto-return for a fixed-wing air unit along a reverse path.
        /// No stops, no spotting checks, no combat — free flight home.
        /// </summary>
        /// <param name="icon">The unit icon GameObject to animate.</param>
        /// <param name="reversePath">World positions to traverse in order (reverse of outbound).</param>
        /// <param name="stepDuration">Duration per hex step.</param>
        /// <param name="onComplete">Callback fired when the full return is complete.</param>
        public static void AnimateAutoReturn(GameObject icon, List<Vector3> reversePath, float stepDuration, Action onComplete)
        {
            try
            {
                if (icon == null || reversePath == null || reversePath.Count == 0)
                {
                    onComplete?.Invoke();
                    return;
                }

                AnimateReturnStep(icon, reversePath, 0, stepDuration, onComplete);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AnimateAutoReturn), e);
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Cancels all active tweens on an icon and snaps it to a position.
        /// Used when ambush or ZoC halts movement mid-path.
        /// </summary>
        public static void CancelAndSnap(GameObject icon, Vector3 snapPosition)
        {
            try
            {
                if (icon == null) return;
                LeanTween.cancel(icon);
                icon.transform.position = snapPosition;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CancelAndSnap), e);
            }
        }

        private static void AnimateReturnStep(GameObject icon, List<Vector3> path, int index,
            float stepDuration, Action onComplete)
        {
            if (index >= path.Count)
            {
                onComplete?.Invoke();
                return;
            }

            LeanTween.move(icon, path[index], stepDuration)
                .setEaseLinear()
                .setOnComplete(() => AnimateReturnStep(icon, path, index + 1, stepDuration, onComplete));
        }
    }
}
